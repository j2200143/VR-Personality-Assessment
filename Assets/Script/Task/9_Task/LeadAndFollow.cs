using UnityEngine;
using System.Collections;

public class LeadAndFollow : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.A4_Cooperation;

    [Header("仲間にする候補のNPC")]
    public GameObject singleNPC;
    public GameObject[] NPCArray;
    private NPC NPCScriptOfchoicedNPC;
    private Animator animator;
    [Header("武器を選ばせる際のメッセージ")]
    public string[] middleMessages;
    private float intervalTime = 2f;
    private float intervalTimeSingle = 4f;
    [Header("武器選択ボタンなど")]
    public GameObject weaponChoiceObject;
    [Header("参照スクリプト")]
    public NPCController nPCControllerSingle;
    public NPCController nPCController;
    [Header("武器によるスコア追加")]
    public int[] scoreWeapon = { 4, 2, 0 };
    public GameObject[] weaponArray;

    //アニメーション設定
    //Animatorの『歩行中かどうか』を制御するboolパラメータ名
    private const string AnimatorJoyParameterName = "isJoy";

    void Start()
    {
        weaponChoiceObject.SetActive(false);
    }
    public void ChoiceFriendNPCSingle()
    {
        NPCScriptOfchoicedNPC = singleNPC.GetComponent<NPC>();
        StartCoroutine(WeaponChoiceSingle());
    }
    private IEnumerator WeaponChoiceSingle()
    {
        //アフターメッセージ表示待ち
        yield return new WaitForSeconds(intervalTimeSingle);
        //プレイヤーにメッセージを表示
        StoryManager.Instance.StartMiddleDialogue(middleMessages);

        //NPCを武器が置かれている近くまで移動させる
        nPCControllerSingle.MoveNPC(0);
        //NPCのメッセージを変更
        singleNPC.GetComponent<NPC>().ShowFixedCanvas("杖が俺には絶対合うぜ。杖がいい");

        //武器選択ボタンなどを表示
        weaponChoiceObject.SetActive(true);

        //NPC.csのShowFixedCanvasAfterTime()によってセリフが上書きされることを防ぐために下記のような強引の方法を用いる
        yield return new WaitForSeconds(5f);
        //NPCのメッセージを変更
        singleNPC.GetComponent<NPC>().ShowFixedCanvas("杖が俺には絶対合うぜ。杖がいい");
    }
    //NoUsed(双子測定Ver)
    //仲間にするNPCを選択
    public void ChoiceFriendNPC(int index)
    {
        //仲間にしたNPCのリアクション
        animator = NPCArray[index].GetComponent<Animator>();
        animator.SetBool(AnimatorJoyParameterName, true); // "Joy" アニメーション開始
        NPCScriptOfchoicedNPC = NPCArray[index].GetComponent<NPC>();
        NPCScriptOfchoicedNPC.ShowFixedCanvasWithTypeSound("やったぜ！これからよろしくな");

        //仲間にしなかったNPCのリアクション
        for (int i = 0; i < NPCArray.Length; i++)
        {
            if (index != i)
            {
                NPCArray[i].GetComponent<NPC>().ShowFixedCanvas("またの機会によろしくな");
            }
        }

        StartCoroutine(WeaponChoice(index));
    }
    private IEnumerator WeaponChoice(int index)
    {
        //NPCのリアクション待ち
        yield return new WaitForSeconds(intervalTime);
        animator.SetBool(AnimatorJoyParameterName, false); // "Joy" アニメーション終了

        //プレイヤーにメッセージを表示
        StoryManager.Instance.StartMiddleDialogue(middleMessages);

        //NPCを武器が置かれている近くまで移動させる
        nPCController.MoveNPC(index);
        //NPCのメッセージを変更
        NPCArray[index].GetComponent<NPC>().ShowFixedCanvas("杖が俺には絶対合うぜ。絶対に杖がいい");

        //武器選択ボタンなどを表示
        weaponChoiceObject.SetActive(true);
    }

    //仲間が装備する武器を選択
    public void ChoiceWeapon(int choiceIndex)
    {
        //スコア加算
        PersonalityManager.Instance.AddFacetScore(personalityFacet, scoreWeapon[choiceIndex]);
        if (choiceIndex == 0)
        {
            //NPCのリアクション
            NPCScriptOfchoicedNPC.ShowFixedCanvasWithTypeSound("やったぜ。杖最高だぜ");
        }
        else
        {
            //NPCのリアクション
            NPCScriptOfchoicedNPC.ShowFixedCanvasWithTypeSound("ああ、俺は杖がよかったな");
        }

        //武器選択ボタンなどを非表示
        weaponChoiceObject.SetActive(false);
        weaponArray[choiceIndex].SetActive(false);
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundWeapon, AudioManager.Instance.Mini);

        //タスクの終了
        StartCoroutine(EndTask());
    }
    private IEnumerator EndTask()
    {
        yield return new WaitForSeconds(3f);
        StoryManager.Instance.MoveNextScene();
    }
}
