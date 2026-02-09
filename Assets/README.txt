README
タスクを新規作成する場合：
1.TaskSectionSOと対応するSceneの作成が必要です。
TaskSectionSOの作成：Create/SO/TaskSectionSOで作成
2.スコア評価時にはScript内で以下関数の呼び出しが必要です。
PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
引数には測定するファセットを設定してください。personalityFacetはPersonalityManager.cs内のenumで定義されています。
3.必要なオブジェクト
Assets/Prefab/Sceneフォルダ内のオブジェクトが基本システムとして必要です。

タスクの増減：
Scene:InitialStoryScene内のStoryManagerオブジェクトのインスペクターにて、
行うタスクに対応するTaskSectionSOのリストの入れ替えによって可能です。

実行モードの変更：
Scene:InitialStoryScene内のStoryManagerオブジェクトのインスペクターにて、以下のモード変更ができます。
・VR実機モード・PC上でVRのシミュレートモード・PC実行モード
推奨：VR実機モード、PC実行モード
プレイ方法：
Scene:InitialStorySceneにて、PC上で実行するかもしくはapkファイルでHMDにインストールを行うとプレイできます。

被験者の結果をcsvファイルとして保存：
Scene:EndStoryScene内のShowScoreUIオブジェクトのインスペクターにて設定してください。

Sprite/UI/空想曲線のspriteの利用、音楽素材の利用にはクレジット表記が必要です。
クレジット表記例：
サイト｜空想曲線
ＵＲＬ｜https://kopacurve.blog.fc2.com/

サイト｜効果音ラボ
ＵＲＬ｜https://soundeffect-lab.info/ 　　 

サイト｜OtoLogic
ＵＲＬ｜https://otologic.jp/
