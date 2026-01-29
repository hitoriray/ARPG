using FixMath;

namespace Battle.ECS.Core
{
    /// <summary>
    /// 战斗逻辑事件，单位秒
    /// </summary>
    public class LogicTime
    {
        /// <summary>
        /// 每帧时间 (秒)
        /// </summary>
        public FP DeltaTime { get; private set; }
        
        /// <summary>
        /// 当前累计时间
        /// </summary>
        public FP Time { get; private set; }
        
        /// <summary>
        /// 真实累计时间，不受时间缩放及暂停影响
        /// </summary>
        public FP RealTime { get; private set; }
        
        /// <summary>
        /// 帧率 (帧/秒)
        /// </summary>
        public int FrameRate { get; private set; }
        
        /// <summary>
        /// 当前累计帧数
        /// </summary>
        public int FrameCount { get; private set; }

        
        public LogicTime(FP deltaTime)
        {
            DeltaTime = deltaTime;
        }

        public void Update()
        {
            Time += DeltaTime;
            FrameCount++;
        }
        
        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            FrameCount = 0;
            Time = 0;
            RealTime = 0;
        }
    }
}
