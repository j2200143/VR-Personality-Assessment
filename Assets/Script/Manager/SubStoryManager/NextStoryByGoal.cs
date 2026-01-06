using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextStoryByGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StoryManager.Instance.MoveNextScene();
        }
    }
}