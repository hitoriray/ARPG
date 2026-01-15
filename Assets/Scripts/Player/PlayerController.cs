using Data;
using JKFrame;
using UnityEngine;

namespace Player
{
    public class PlayerController : SingletonMono<PlayerController>
    {
        [SerializeField] private PlayerView playerView;

        public void Init()
        {
            playerView.InitOnGame(DataManager.CustomCharacterData);
        }
    }
}
