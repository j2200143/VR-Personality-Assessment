using UnityEngine;
/// <summary>
///SearchableItemと連携して、プレイヤーが無くしものを探したかを判定するスクリプト
/// </summary>
public class Task_LostItem_O3 : MonoBehaviour
{
    public static Task_LostItem_O3 Instance { get; private set; }

    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.O3_Emotionality;
    [Tooltip("落とし物を探したプレイヤーの得点")]
    public int score = 1;
    [Tooltip("探す素振りをした判定としてダミーに何個インタラクトしたか")]
    public int borderLineCount = 2;

    [Header("無くし物が見つかる前のリアクションをするNPC")]
    public GameObject npcObject_before;
    [Header("無くし物が見つかった際のリアクションをするNPC")]
    public GameObject npcObject_after;//このNPCが選択肢を持つ


    private int dummyCount = 0;
    private bool isFound = false;
    private bool isScored = false;

    void Awake()
    {
        //インスタンス化
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        npcObject_after.SetActive(false);
    }

    //対象物を見つけた場合
    public void OnFindTarget()
    {
        npcObject_before.SetActive(false);
        npcObject_after.SetActive(true);
        isFound = true;
    }

    //ダミーだった場合
    public void OnCheckDummy()
    {
        dummyCount++;
    }

    //タスク終了時にdummyCountの数に応じてスコア送信を行う
    //タスク終了となるNPCに話しかけられる距離に入った場合
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isScored)
            {
                isScored = true;
                //無くし物を見つけた場合
                if (isFound)
                {
                    score = 2;
                }
                else if (dummyCount >= borderLineCount)//見つけてはいないが、探した場合
                {
                    score = 1;
                }
                PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
            }
        }
    }
}
