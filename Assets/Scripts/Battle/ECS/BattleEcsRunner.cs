using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Attribute;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Features;
using Battle.ECS.View;
using Boss;
using FixMath;
using RayPlayer;
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

        public Entity RegisterPlayer(PlayerController player)
        {
            if (Context == null || player == null)
                return Entity.Null;

            var viewComp = player.GetComponentInChildren<PlayerView>();
            var view = (ICharacterView)viewComp;
            var viewObj = viewComp != null ? viewComp.gameObject : player.gameObject;
            var position = (TSVector3)viewObj.transform.position;
            var rotation = (TSQuaternion)viewObj.transform.rotation;
            var playerAttr = player.CharacterAttribute;
            var attribute = new Component.Attribute
            {
                Attack = (FP)playerAttr.attack.Total,
                MaxHp = (FP)playerAttr.maxHp.Total,
                MaxMp = (FP)playerAttr.maxMp.Total,
                Defense = (FP)(player.CharacterConfig != null ? player.CharacterConfig.defenseBaseValue : 0f),
            };
            var health = new Health((FP)playerAttr.currentHp, (FP)playerAttr.currentHp);
            if (_playerEntity.IsAlive() == false)
            {
                _playerEntity = Context.World.Create(
                    new Battle.ECS.Component.PlayerComp(0),
                    new Position(position),
                    new Rotation(rotation),
                    new ViewReference(viewObj, view),
                    new SyncFromView(),
                    attribute,
                    health,
                    new BuffList(16)
                );
            }
            else
            {
                _playerEntity.Replace(new Position(position));
                _playerEntity.Replace(new Rotation(rotation));
                _playerEntity.Replace(new ViewReference(viewObj, view));
                _playerEntity.Replace(health);
                _playerEntity.Replace(attribute);
                _playerEntity.Replace(new BuffList(16));
                _playerEntity.Replace(new SyncFromView());
            }

            return _playerEntity;
        }

        /// <summary>
        /// 注册 Boss 实体到 ECS 世界
        /// </summary>
        public Entity RegisterBoss(BossController boss)
        {
            if (Context == null || boss == null)
                return Entity.Null;

            var viewObj = boss.gameObject;
            var bossAttr = boss.CharacterAttribute;
            var position = (TSVector3)viewObj.transform.position;
            var rotation = (TSQuaternion)viewObj.transform.rotation;
            
            var attribute = new Component.Attribute
            {
                Attack = (FP)bossAttr.attack.Total,
                MaxHp = (FP)bossAttr.maxHp.Total,
                MaxMp = (FP)bossAttr.maxMp.Total,
                Defense = (FP)(boss.CharacterConfig != null ? boss.CharacterConfig.defenseBaseValue : 0f),
            };
            var health = new Health((FP)bossAttr.currentHp, (FP)bossAttr.maxHp.Total);

            var entity = Context.World.Create(
                new BossTag(0),
                new Position(position),
                new Rotation(rotation),
                new ViewReference(viewObj, null),
                new SyncFromView(),
                attribute,
                health,
                new BuffList(16)
            );

            return entity;
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
