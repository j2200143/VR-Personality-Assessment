using UnityEngine;

public enum ContainerType
{
    Right, // 右側用
    Left // 左側用
}

public class ContainerInfo : MonoBehaviour
{
    [Header("コンテナの種類")]
    public ContainerType type;
}