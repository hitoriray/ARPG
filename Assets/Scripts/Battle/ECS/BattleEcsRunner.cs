using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Features;
using Battle.ECS.View;
using FixMath;
using Player;
using UnityEngine;

namespace Battle.ECS
{
    /// <summary>
    /// ECS战斗入口 - 驱动逻辑与表现
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class BattleEcsRunner : MonoBehaviour
    {
        public static BattleEcsRunner Instance { get; private set; }

        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private int logicFrameRate = 20;
        [SerializeField] private bool dontDestroyOnLoad = true;

        public LocalBattleContext Context { get; private set; }
        private LocalLogicFeature _logicFeature;
        private LocalViewFeature _viewFeature;
        private float _accumulator;

        private Entity _playerEntity;

        public static BattleEcsRunner Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[Battle.ECS]");
            return go.AddComponent<BattleEcsRunner>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Update()
        {
            if (Context == null || Context.State.Value != BattleState.Running)
                return;

            _accumulator += Time.deltaTime;
            var dt = (float)Context.LogicTime.DeltaTime;
            while (_accumulator >= dt)
            {
                _accumulator -= dt;
                Context.LogicTime.Update();
                _logicFeature.Update();
                _logicFeature.Cleanup();
            }

            _viewFeature.Update();
            _viewFeature.Cleanup();
        }

        private void OnDestroy()
        {
            Shutdown();
            if (Instance == this)
                Instance = null;
        }

        public void RegisterPlayer(PlayerController player)
        {
            if (Context == null || player == null) return;

            var viewComp = player.GetComponentInChildren<PlayerView>();
            var view = viewComp as ICharacterView;
            var viewObj = viewComp != null ? viewComp.gameObject : player.gameObject;
            var position = (TSVector3)viewObj.transform.position;
            var rotation = (TSQuaternion)viewObj.transform.rotation;

            if (_playerEntity.IsAlive() == false)
            {
                _playerEntity = Context.World.Create(
                    new Battle.ECS.Component.Player(0),
                    new Position(position),
                    new Rotation(rotation),
                    new ViewReference(viewObj, view),
                    new SyncFromView()
                );
            }
            else
            {
                _playerEntity.Set(new Position(position));
                _playerEntity.Set(new Rotation(rotation));
                _playerEntity.Set(new ViewReference(viewObj, view));
                if (!_playerEntity.Has<SyncFromView>())
                    _playerEntity.Add<SyncFromView>();
            }
        }

        private void Initialize()
        {
            if (Context != null) return;

            var logicDeltaTime = FP.FromFloat(1f / logicFrameRate);
            Context = new LocalBattleContext(randomSeed, logicDeltaTime);
            _logicFeature = new LocalLogicFeature(Context);
            _viewFeature = new LocalViewFeature(Context);

            _logicFeature.Initialize();
            _logicFeature.SubscribeEvents();
            _viewFeature.Initialize();
            _viewFeature.SubscribeEvents();

            _viewFeature.LoadView(new BattleViewReference
            {
                Camera = Camera.main
            });

            Context.State.Value = BattleState.Running;
        }

        private void Shutdown()
        {
            if (Context == null) return;

            _viewFeature.UnloadView();
            _viewFeature.Shutdown();
            _logicFeature.Shutdown();
            _viewFeature = null;
            _logicFeature = null;

            Context.Dispose();
            Context = null;
        }
    }
}
