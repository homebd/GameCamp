using UnityEngine;

namespace GameCamp.Game.Snake
{
    public class SnakeSpawnDirector : MonoBehaviour
    {
        [SerializeField] private SnakeController snakeController;

        public void StartSnake()
        {
            if (snakeController == null)
            {
                return;
            }

            snakeController.Begin();
        }
    }
}

