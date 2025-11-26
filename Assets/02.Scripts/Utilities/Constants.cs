public static class Constants
{
    // 씬의 종류
    public enum SceneType
    {
        TITLE,
        SET_PLAYERNAME,
        PROLOGUE,
        STAGE_1, RECALL_1,
        STAGE_2, RECALL_2,
        STAGE_3, RECALL_3,
        STAGE_3_2, STAGE_4_1, RECALL_4,
        STAGE_4_2, STAGE_4_3,
        STAGE_5, RECALL_5,
        ENDING_HAPPY, ENDING_BAD
    }

    // SceneType -> 실제 씬 파일 이름(string)
    public static string ToSceneName(this SceneType type)
    {
        switch (type)
        {
            case SceneType.TITLE: return "TitleScene";
            case SceneType.SET_PLAYERNAME: return "SetPlayerName";
            case SceneType.PROLOGUE: return "Prologue";
            case SceneType.STAGE_1: return "StageScene1";
            case SceneType.RECALL_1: return "RecallScene1";
            case SceneType.STAGE_2: return "StageScene2";
            case SceneType.RECALL_2: return "RecallScene2";
            case SceneType.STAGE_3: return "StageScene3";
            case SceneType.RECALL_3: return "RecallScene3";
            case SceneType.STAGE_3_2: return "StageScene3_2";
            case SceneType.STAGE_4_1: return "StageScene4_1";
            case SceneType.RECALL_4: return "RecallScene4";
            case SceneType.STAGE_4_2: return "StageScene4_2";
            case SceneType.STAGE_4_3: return "StageScene4_3";
            case SceneType.STAGE_5: return "StageScene5";
            case SceneType.RECALL_5: return "RecallScene5";
            case SceneType.ENDING_HAPPY: return "Ending_Happy";
            case SceneType.ENDING_BAD: return "Ending_Bad";
            default: return null;
        }
    }

    // 씬 파일 이름(string) → SceneType 변환
    public static SceneType ToSceneType(this string sceneName)
    {
        switch (sceneName)
        {
            case "TitleScene": return SceneType.TITLE;
            case "SetPlayerName": return SceneType.SET_PLAYERNAME;
            case "Prologue": return SceneType.PROLOGUE;
            case "StageScene1": return SceneType.STAGE_1;
            case "RecallScene1": return SceneType.RECALL_1;
            case "StageScene2": return SceneType.STAGE_2;
            case "RecallScene2": return SceneType.RECALL_2;
            case "StageScene3": return SceneType.STAGE_3;
            case "RecallScene3": return SceneType.RECALL_3;
            case "StageScene3_2": return SceneType.STAGE_3_2;
            case "StageScene4_1": return SceneType.STAGE_4_1;
            case "RecallScene4": return SceneType.RECALL_4;
            case "StageScene4_2": return SceneType.STAGE_4_2;
            case "StageScene4_3": return SceneType.STAGE_4_3;
            case "StageScene5": return SceneType.STAGE_5;
            case "RecallScene5": return SceneType.RECALL_5;
            case "Ending_Happy": return SceneType.ENDING_HAPPY;
            case "Ending_Bad": return SceneType.ENDING_BAD;
            default: return SceneType.TITLE;
        }
    }

    // 대화창의 종류
    public enum DialogueType { PLAYER_TALKING, PLAYER_THINKING, NPC, MONOLOG, PLAYER_BUBBLE, NPC_BUBBLE }
    public static int ToInt(this DialogueType dialogueType)
    {
        // 다이얼로그 타입에 따라 다른 숫자 반환
        switch (dialogueType)
        {
            case DialogueType.PLAYER_TALKING: return 0;
            case DialogueType.PLAYER_THINKING: return 1;
            case DialogueType.NPC: return 2;
            case DialogueType.MONOLOG: return 3;
            case DialogueType.PLAYER_BUBBLE: return 4;
            case DialogueType.NPC_BUBBLE: return 5;
            default: return 0;
        }
    }

    public enum SoundType { BGM, LOOP, SOUND_EFFECT }
    // 사운드 종류
    // 1. 배경음
    public const int
        BGM_STOP = -1,
        BGM_TITLE = 0,
        BGM_TITLESOUND=1,
        BGM_PROLOGUE_0 = 2,
        BGM_STAGE1 = 3,
        BGM_REMINISCENE1 = 4,
        BGM_STAGE2 = 5,
        BGM_REMINISCENE2 = 6;

    // 2. 일반 오브젝트 효과음
    public const int
        Sound_WormholeActived = 1,
        Sound_FrontDoorOpenAndClose = 2,
        Sound_RoomDoorOpenAndClose = 3,
        Sound_birdChirps = 4,
        Sound_catMeow = 5,
        Sound_catMeow2 = 6;



    // 4. 루프 (반복 되어야 하는 것)
    public const int
        Sound_FootStep_CAT_VERSION = 0,
        Sound_FootStep_HUMAN_VERSION = 1;

    // 5. 그 외
    public const int
        Sound_Typing = -1,
        Sound_Click = 0;

}