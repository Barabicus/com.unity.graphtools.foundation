using System;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base class for implementations of <see cref="IStateComponent"/>.
    /// </summary>
    /// <remarks>
    /// <p>
    /// State components should expose a readonly interface. All updates to a state component
    /// should be done through an updater, which is an instance of a class deriving from <see cref="BaseUpdater{T}"/>.
    /// This ensures that change tracking is done properly.
    /// </p>
    /// <p>
    /// State components are serialized and unserialized on undo/redo. Make sure that their fields behave properly
    /// under these conditions. Either put the Serialized attribute on them or check that they are initialized
    /// before accessing them.
    /// </p>
    /// <p>
    /// Do not derive directly from this class. Instead, use <see cref="AssetStateComponent{TUpdater}"/>,
    /// <see cref="AssetViewStateComponent{TUpdater}"/> or <see cref="ViewStateComponent{TUpdater}"/> as the base class.
    /// </p>
    /// </remarks>
    [Serializable]
    public abstract class StateComponent<TUpdater> : IStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        /// <summary>
        /// Updater class for the state component.
        /// </summary>
        /// <typeparam name="TStateComponent">The type of state component that is to be updated.</typeparam>
        public abstract class BaseUpdater<TStateComponent> : IStateComponentUpdater where TStateComponent : StateComponent<TUpdater>
        {
            /// <summary>
            /// The state component that can be updated through this updater.
            /// </summary>
            protected TStateComponent m_State;

            public void Initialize(IStateComponent state)
            {
                Assert.IsNull(m_State, "Missing Dispose call.");

                m_State = state as TStateComponent;
                BeginStateChange();
            }

            ~BaseUpdater()
            {
                Dispose(false);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                Assert.IsNotNull(m_State, "Missing Initialize call.");
                Dispose(true);
                m_State = null;
            }

            /// <summary>
            /// Dispose implementation.
            /// </summary>
            /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
            /// Otherwise it is called from the finalizer.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                    EndStateChange();
            }

            void BeginStateChange()
            {
                Assert.IsTrue(
                    StateObserverHelper.CurrentObserver == null ||
                    StateObserverHelper.CurrentObserver.ModifiedStateComponents.Contains(m_State.StateSlotName),
                    $"Observer {StateObserverHelper.CurrentObserver?.GetType()} does not specify that it modifies {m_State.StateSlotName}. Please add the state component to its {nameof(IStateObserver.ModifiedStateComponents)}.");

                m_State.PushChangeset(m_State.CurrentVersion);
            }

            void EndStateChange()
            {
                // unchecked: wrap around on overflow without exception.
                unchecked
                {
                    m_State.CurrentVersion++;
                }
            }

            /// <summary>
            /// Force the state component to ask its observers to do a complete update.
            /// </summary>
            public void ForceCompleteUpdate()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        uint m_EarliestChangeSetVersion;
        UpdateType m_UpdateType = UpdateType.None;
        TUpdater m_Updater;

        public uint CurrentVersion { get; private set; } = 1;

        [SerializeField]
        string m_StateSlotName;

        /// <inheritdoc/>
        public string StateSlotName
        {
            get => m_StateSlotName;
            set => m_StateSlotName = value;
        }

        [Obsolete("Use UpdateScope instead. Added in 0.9+. (UnityUpgradable) -> UpdateScope")]
        public TUpdater Updater => UpdateScope;

        /// <summary>
        /// The updater for the state component.
        /// </summary>
        /// <remarks>Since state component expose a read only interfaces, all modifications
        /// to a state component need to be done through this Updater.</remarks>
        public TUpdater UpdateScope
        {
            get
            {
                if (m_Updater == null)
                    m_Updater = new TUpdater();

                m_Updater.Initialize(this);
                return m_Updater;
            }
        }

        ~StateComponent()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
        /// Otherwise it is called from the finalizer.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Push the current changeset and tag it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number associated with the changeset.</param>
        protected virtual void PushChangeset(uint version) { }

        /// <summary>
        /// Purge the changesets that track changes up to and including untilVersion.
        /// </summary>
        /// <remarks>
        /// The state component can choose to purge more recent changesets.
        /// </remarks>
        /// <param name="untilVersion">Version up to which we should purge changesets. Pass uint.MaxValue to purge all changesets.</param>
        public virtual void PurgeOldChangesets(uint untilVersion)
        {
            m_EarliestChangeSetVersion = Math.Min(untilVersion, CurrentVersion);
            if (m_EarliestChangeSetVersion == CurrentVersion)
                m_UpdateType = UpdateType.None;
        }

        /// <inheritdoc/>
        public bool HasChanges()
        {
            return m_EarliestChangeSetVersion != CurrentVersion;
        }

        /// <summary>
        /// Set how the observers should update themselves. Unless <paramref name="force"/> is true,
        /// if the update type is already set, it will be changed only
        /// if the new update type has a higher value than the current one.
        /// </summary>
        /// <param name="type">The update type.</param>
        /// <param name="force">Set the update type even if the new value is lower than the current one.</param>
        protected internal void SetUpdateType(UpdateType type, bool force = false)
        {
            if (type > m_UpdateType || force)
                m_UpdateType = type;
        }

        /// <inheritdoc/>
        public UpdateType GetUpdateType(StateComponentVersion observerVersion)
        {
            if (observerVersion.HashCode != GetHashCode())
            {
                return UpdateType.Complete;
            }

            // If view is new or too old, tell it to rebuild itself completely.
            if (observerVersion.Version == 0 || observerVersion.Version < m_EarliestChangeSetVersion)
            {
                return UpdateType.Complete;
            }

            // This is safe even if Version wraps around after an overflow.
            return observerVersion.Version == CurrentVersion ? UpdateType.None : m_UpdateType;
        }

        /// <inheritdoc cref="IStateComponent.BeforeSerialize"/>
        public virtual void BeforeSerialize() { }

        /// <inheritdoc cref="IStateComponent.AfterDeserialize"/>
        public virtual void AfterDeserialize() { }

        /// <summary>
        /// Performs state validation after deserialization.
        /// </summary>
        public virtual void ValidateAfterDeserialize() { }
    }
}