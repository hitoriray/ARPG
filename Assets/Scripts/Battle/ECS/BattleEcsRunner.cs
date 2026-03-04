using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Features;
using Battle.ECS.View;
using FixMath;
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
        
        public Entity RegisterCharacter(ICharacter character)
        {
            if (Context == null || character == null)
                return Entity.Null;

            var viewComp = character.ModelTransform.GetComponentInChildren<ICharacterView>();
            var viewObj = viewComp != null ? viewComp.gameObject : character.ModelTransform.gameObject;
            var position = (TSVector3)viewObj.transform.position;
            var rotation = (TSQuaternion)viewObj.transform.rotation;
            var characterAttr = character.CharacterAttribute;
            var attribute = new Component.Attribute
            {
                Attack = (FP)characterAttr.attack.Total,
                MaxHp = (FP)characterAttr.maxHp.Total,
                MaxMp = (FP)characterAttr.maxMp.Total,
                Defense = (FP)(character.CharacterConfig != null ? character.CharacterConfig.defenseBaseValue : 0f),
            };
            var health = new Health((FP)characterAttr.currentHp, (FP)characterAttr.maxHp.Total);

            Entity entity = Entity.Null;
            if (character.IsPlayerControlled && _playerEntity.IsAlive() == false)
            {
                _playerEntity = Context.World.Create(
                    new PlayerComp(0),
                    new Position(position),
                    new Rotation(rotation),
                    new ViewReference(viewObj),
                    new SyncFromView(),
                    attribute,
                    health,
                    new BuffList(16)
                );
                entity = _playerEntity;
            }
            else if (character.IsPlayerControlled == false)
            {
                entity = Context.World.Create(
                    new BossTag(0),
                    new Position(position),
                    new Rotation(rotation),
                    new ViewReference(viewObj),
                    new SyncFromView(),
                    attribute,
                    health,
                    new BuffList(16)
                );
            }

            return entity;
        }

        /// <summary>
        /// 直接对玩家 ECS Health 组件加血（供消耗品等非战斗伤害路径使用）。
        /// ViewSyncSystem 每帧将 Health.Current 同步回 CharacterAttribute，
        /// 因此只更新 ECS 即可，无需再调用 CharacterAttribute.SetHp。
        /// </summary>
        public void HealPlayer(float amount)
        {
            if (Context == null || !_playerEntity.IsAlive()) return;

            ref var health = ref Context.World.Get<Health>(_playerEntity);
            health.Current = TSMath.Clamp(health.Current + (FP)amount, FP.Zero, health.Max);
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
