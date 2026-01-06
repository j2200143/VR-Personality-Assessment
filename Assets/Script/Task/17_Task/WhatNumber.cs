using UnityEngine;

/// <summary>
/// 並び替えるアイテムにアタッチし、
/// そのアイテムが正解スロットの何番目 (0から) に入るべきかを設定します。
/// </summary>
public class WhatNumber : MonoBehaviour
{
    [Tooltip("このアイテムが正解スロットの何番目 (0から) に入るべきか")]
    public int whatNumber;
}