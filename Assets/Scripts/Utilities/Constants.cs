public static class Constants
{
    //// 씬의 종류
    //public enum SceneType { START, ROOM_1, FOLLOW_1, ROOM_2, FOLLOW_2, ENDING }
    //public static int ToInt(this SceneType sceneType)
    //{
    //    switch (sceneType)
    //    {
    //        case SceneType.START: return 0;
    //        case SceneType.ROOM_1: return 1;
    //        case SceneType.FOLLOW_1: return 2;
    //        case SceneType.ROOM_2: return 3;
    //        case SceneType.FOLLOW_2: return 4;
    //        case SceneType.ENDING: return 5;
    //        default: return 0;
    //    }
    //}
    //public static SceneType ToEnum(this int sceneType)
    //{
    //    switch (sceneType)
    //    {
    //        case 0: return SceneType.START;
    //        case 1: return SceneType.ROOM_1;
    //        case 2: return SceneType.FOLLOW_1;
    //        case 3: return SceneType.ROOM_2;
    //        case 4: return SceneType.FOLLOW_2;
    //        case 5: return SceneType.ENDING;
    //        default: return 0;
    //    }
    //}

    // 대화창의 종류
    public enum DialogueType { PLAYER_TALKING, PLAYER_THINKING, NPC, CENTER }
    public static int ToInt(this DialogueType dialogueType)
    {
        // 다이얼로그 타입에 따라 다른 숫자 반환
        switch (dialogueType)
        {
            case DialogueType.PLAYER_TALKING: return 0;
            case DialogueType.PLAYER_THINKING: return 1;
            case DialogueType.NPC: return 2;
            //case DialogueType.CENTER: return 3;
            default: return 0;
        }
    }


}