using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
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
    public sealed class BattleEcsRunner : MonoSingleton<BattleEcsRunner>
    {
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private int logicFrameRate = 20;

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

        protected override void Awake()
        {
            base.Awake();
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Shutdown();
        }
        
        public Entity RegisterCharacter(ICharacter character)
        {
            if (character == null)
                return Entity.Null;
            if (!EnsureContextReady())
                return Entity.Null;
            if (character.ModelTransform == null)
            {
                Debug.LogWarning("[BattleEcsRunner] RegisterCharacter failed: character.ModelTransform is null.");
                return Entity.Null;
            }

            var viewComp = character.ModelTransform.GetComponentInChildren<ICharacterView>();
            var viewObj = viewComp != null ? viewComp.gameObject : character.ModelTransform.gameObject;
            var position = (TSVector3)viewObj.transform.position;
            var rotation = (TSQuaternion)viewObj.transform.rotation;
            var characterAttr = character.CharacterAttribute;
            var attribute = new Component.Attribute
            {
                Attack = (FP)(characterAttr != null ? characterAttr.attack.Total : 0f),
                MaxHp = (FP)(characterAttr != null ? characterAttr.maxHp.Total : 0f),
                MaxMp = (FP)(characterAttr != null ? characterAttr.maxMp.Total : 0f),
                Defense = (FP)(character.CharacterConfig != null ? character.CharacterConfig.defenseBaseValue : 0f),
            };
            var health = new Health(
                (FP)(characterAttr != null ? characterAttr.currentHp : 0f),
                (FP)(characterAttr != null ? characterAttr.maxHp.Total : 0f));

            Entity entity = Entity.Null;
            if (character.IsPlayerControlled)
            {
                if (!IsEntityAliveSafe(_playerEntity))
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
                }
                else
                {
                    // Keep existing player entity and refresh runtime-bound data after scene/model changes.
                    _playerEntity.Replace(new Position(position));
                    _playerEntity.Replace(new Rotation(rotation));
                    _playerEntity.Replace(new ViewReference(viewObj));
                    _playerEntity.Replace(new SyncFromView());
                    _playerEntity.Replace(attribute);
                    _playerEntity.Replace(health);
                }

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
            if (Context == null || Context.World == null || !IsEntityAliveSafe(_playerEntity))
                return;

            ref var health = ref Context.World.Get<Health>(_playerEntity);
            health.Current = TSMath.Clamp(health.Current + (FP)amount, FP.Zero, health.Max);
        }

        private void Initialize()
        {
            if (Context != null && Context.World != null) return;

            var logicDeltaTime = FP.FromFloat(1f / logicFrameRate);
            Context = new LocalBattleContext(randomSeed, logicDeltaTime);
            _logicFeature = new LocalLogicFeature(Context);
            _viewFeature = new LocalViewFeature(Context);
            _playerEntity = Entity.Null;

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
            if (_viewFeature != null)
            {
                _viewFeature.UnloadView();
                _viewFeature.Shutdown();
            }
            if (_logicFeature != null)
            {
                _logicFeature.Shutdown();
            }
            _viewFeature = null;
            _logicFeature = null;

            Context?.Dispose();
            Context = null;
            _playerEntity = Entity.Null;
        }

        private bool EnsureContextReady()
        {
            if (Context != null && Context.World != null && _logicFeature != null && _viewFeature != null)
                return true;

            Shutdown();
            Initialize();
            return Context != null && Context.World != null;
        }

        private static bool IsEntityAliveSafe(Entity entity)
        {
            try
            {
                return entity.IsAlive();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[BattleEcsRunner] Invalid entity handle detected, treat as dead: " +
                    $"[{entity.WorldId}:{entity.Id}:{entity.Version}] ex={ex.GetType().Name}");
                return false;
            }
        }
    }
}
