using UnityEngine;

//例：実際にプレイヤーが薬を届けたか判定する。
public class TriggerNPC : MonoBehaviour
{
    public PersonalityFacet personalityFacet = PersonalityFacet.A3_Altruism;
    public int addScore;
    public NPC npc;
    public string message;
    public string animationType = "isJoy";

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤー (Rigidbody と "Player" タグが必要) が入ってきた
        if (other.CompareTag("Player") && npc != null)
        {
            //NPCのメッセージ表示
            npc.ShowFixedCanvasWithTypeSound(message);
            //NPCのアニメーション変更
            npc.animator.SetBool(animationType, true);
            //スコア送信
            PersonalityManager.Instance.AddFacetScore(personalityFacet, addScore);
            //再度アクセスできないようにする
            this.gameObject.SetActive(false);
        }
    }
}
