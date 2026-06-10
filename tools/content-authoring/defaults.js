window.JOSEON_AUTHORING_DEFAULTS = {
  "version": 1,
  "updatedAt": "2026-06-09T08:50:02.998Z",
  "project": {
    "title": "조선 무협 SRPG",
    "note": "콘텐츠 편집기에서 저장하면 Unity Resources의 게임 대사에 즉시 반영됩니다."
  },
  "characters": [
    {
      "id": "park_sungjun",
      "displayName": "박성준",
      "role": "백두천광검문 소문주 · 빛/검",
      "portraitId": "",
      "portraitResource": "",
      "notes": "20세. 주인공.",
      "age": 20,
      "sectId": "baekdu_light_sword",
      "sectName": "백두천광검문"
    },
    {
      "id": "park_mugyeom",
      "displayName": "박무겸",
      "role": "병든 문주",
      "portraitId": "",
      "portraitResource": "",
      "notes": "백두천광검문의 현 문주.",
      "age": 62,
      "sectId": "baekdu_light_sword",
      "sectName": "백두천광검문"
    },
    {
      "id": "yeon_ok",
      "displayName": "연옥",
      "role": "엄격한 사범",
      "portraitId": "",
      "portraitResource": "",
      "notes": "성준을 단련시키는 사범.",
      "age": 0,
      "sectId": "baekdu_light_sword",
      "sectName": "백두천광검문"
    },
    {
      "id": "cho_hui",
      "displayName": "초희",
      "role": "소백촌 약방",
      "portraitId": "",
      "portraitResource": "",
      "notes": "초반 생계와 약재 루프의 연결 인물.",
      "age": 0,
      "sectId": "sobaek_village",
      "sectName": "소백약방"
    },
    {
      "id": "baek_ryeon",
      "displayName": "백련",
      "role": "설악창문 · 서리/창",
      "portraitId": "",
      "portraitResource": "",
      "notes": "강원 설악창문.",
      "age": 17,
      "sectId": "seorak_spear",
      "sectName": "설악창문"
    },
    {
      "id": "do_arin",
      "displayName": "도아린",
      "role": "화왕도문 · 불/도",
      "portraitId": "",
      "portraitResource": "",
      "notes": "경상 화왕도문.",
      "age": 18,
      "sectId": "hwawang_blade",
      "sectName": "화왕도문"
    },
    {
      "id": "jin_seoyul",
      "displayName": "진서율",
      "role": "천뢰봉문 · 전기/봉",
      "portraitId": "",
      "portraitResource": "",
      "notes": "경성 천뢰봉문.",
      "age": 16,
      "sectId": "cheonroe_staff",
      "sectName": "천뢰봉문"
    },
    {
      "id": "seo_a",
      "displayName": "신서아",
      "role": "화접풍류문 · 바람/꽃/부채",
      "portraitId": "",
      "portraitResource": "",
      "notes": "13세. 전라 화접풍류문.",
      "age": 13,
      "sectId": "hwajeop_fan",
      "sectName": "화접풍류문"
    },
    {
      "id": "han_biyeon",
      "displayName": "한비연",
      "role": "흑련암문 · 어둠/독/암기",
      "portraitId": "",
      "portraitResource": "",
      "notes": "황해 흑련암문.",
      "age": 15,
      "sectId": "heukryeon_shadow",
      "sectName": "흑련암문"
    }
  ],
  "backgrounds": [
    {
      "id": "joseon_murim_game_map",
      "title": "조선-중원 강호도",
      "resourcePath": "WorldMap/joseon_murim_game_map",
      "previewUrl": "/resources/WorldMap/joseon_murim_game_map.png",
      "notes": "통합 월드맵"
    }
  ],
  "portraits": [],
  "props": [],
  "dialogueScenes": [
    {
      "id": "chapter1_prologue",
      "title": "제1장 · 꺼져가는 천광",
      "location": "백두산 백두천광검문",
      "backgroundId": "joseon_murim_game_map",
      "startNodeId": "chapter1_prologue_001",
      "entries": [
        {
          "id": "c1_000",
          "speakerId": "",
          "line": "백두산 중턱. 눈은 쌓였고, 검각은 기울었고, 백두천광검문의 깃발은 바람에 ‘나 아직 안 죽었다’는 듯 겨우 펄럭인다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "choices": []
        },
        {
          "id": "c1_010",
          "speakerId": "",
          "line": "한때는 북방의 명문. 지금은 병든 문주, 무서운 사범, 그리고 지붕 위에서 낮잠 자는 소문주 하나가 전 재산이다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "choices": []
        },
        {
          "id": "c1_020",
          "speakerId": "yeon_ok",
          "line": "박성준. 내려와라. 지붕은 연무장이 아니고, 네 이불도 아니다.",
          "mood": "엄격",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_030",
          "speakerId": "park_sungjun",
          "line": "사범님, 낮잠이 아닙니다. 하늘의 기운을 받아 천광심법을... 음, 누워서 받는 중이었습니다.",
          "mood": "능청",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_040",
          "speakerId": "yeon_ok",
          "line": "좋다. 그럼 누운 채로 장작도 패고, 목인도 서른 번 치거라.",
          "mood": "차갑게",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_050",
          "speakerId": "park_sungjun",
          "line": "(어떻게 둘러댈까?)",
          "mood": "선택",
          "backgroundId": "",
          "choices": [
            {
              "id": "c1_060",
              "text": "하늘 기운이 아직 덜 내려왔습니다.",
              "disposition": 3,
              "targetEntryId": "c1_060",
              "flagAdded": "CH1_JOKED_DREAM",
              "approvalId": "",
              "approvalDelta": 0,
              "factionId": "",
              "factionDelta": 0,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "id": "c1_060",
              "text": "검도 사람도 휴식이 있어야 빛납니다.",
              "disposition": 1,
              "targetEntryId": "c1_060",
              "flagAdded": "CH1_JOKED_BLADE_REST",
              "approvalId": "",
              "approvalDelta": 0,
              "factionId": "",
              "factionDelta": 0,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "id": "c1_060",
              "text": "사범님 발소리 듣고 이미 마음은 내려갔습니다.",
              "disposition": 0,
              "targetEntryId": "c1_060",
              "flagAdded": "CH1_JOKED_WAITING",
              "approvalId": "",
              "approvalDelta": 0,
              "factionId": "",
              "factionDelta": 0,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "id": "c1_060",
          "speakerId": "yeon_ok",
          "line": "입공만 보면 벌써 천하제일이다. 몸도 내려와라. 문주님이 찾으신다.",
          "mood": "단호",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_070",
          "speakerId": "",
          "line": "성준이 뛰어내리자 기와 세 장이 먼저 하산했다. 연옥의 눈썹도 같이 올라갔다.",
          "mood": "서술",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_080",
          "speakerId": "park_sungjun",
          "line": "보셨죠? 제 경공이 아니라 검각이 먼저 움직였습니다. 건물이 아주 적극적이에요.",
          "mood": "농담",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_090",
          "speakerId": "yeon_ok",
          "line": "다음엔 네 용돈이 먼저 하산할 것이다.",
          "mood": "꾸짖음",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_100",
          "speakerId": "park_mugyeom",
          "line": "성준아. 검은 폼으로 드는 게 아니다. 지켜야 할 것이 있을 때 비로소 손에 붙는다.",
          "mood": "조용함",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_110",
          "speakerId": "park_sungjun",
          "line": "아버지, 저는 폼도 지키고 문파도 지키고 밥상도 지키고 싶은데요. 셋 중 밥상이 제일 위태롭습니다.",
          "mood": "능청",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_120",
          "speakerId": "park_mugyeom",
          "line": "그래서 부른 것이다. 중원 문파들이 백두산 영맥을 눈독 들인다는 말이 돈다.",
          "mood": "걱정",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_130",
          "speakerId": "park_mugyeom",
          "line": "천광심법과 백야검결, 이제 네 차례다. 문파가 작아졌다고 이름까지 작게 부르지는 마라.",
          "mood": "당부",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_140",
          "speakerId": "park_sungjun",
          "line": "걱정 마세요. 제가 가벼운 건 말투뿐입니다. 건드리면, 꽤 무겁게 받아칠 겁니다.",
          "mood": "다짐",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_150",
          "speakerId": "",
          "line": "낮이 되자 성준은 소백촌으로 내려갔다. 마을 사람들은 그를 ‘사고뭉치 도련님’이라 부르면서도, 은근히 길을 터준다.",
          "mood": "서술",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_160",
          "speakerId": "cho_hui",
          "line": "왔네, 백두산 대표 한량. 오늘은 지붕 말고 땅 밟고 다니네?",
          "mood": "핀잔",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_170",
          "speakerId": "park_sungjun",
          "line": "초희야, 그 말투... 나를 기다린 사람의 향기가 난다.",
          "mood": "풍류",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_180",
          "speakerId": "cho_hui",
          "line": "기다렸지. 장작더미가. 약초 바구니도. 그리고 네가 미룬 외상 장부도.",
          "mood": "현실적",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_190",
          "speakerId": "",
          "line": "문파 부흥은 거창한 비급에서 시작되지 않았다. 장작, 약초, 지붕 수리. 듣기엔 초라해도 은전은 거짓말을 하지 않는다.",
          "mood": "서술",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_200",
          "speakerId": "park_sungjun",
          "line": "(오늘은 뭘 해야 덜 혼나고 더 벌까?)",
          "mood": "선택",
          "backgroundId": "",
          "choices": [
            {
              "id": "c1_210__.",
              "text": "장작부터 패자. 백야검결로 나무도 감동시켜보자.",
              "disposition": 1,
              "targetEntryId": "c1_210",
              "flagAdded": "CH1_VILLAGE_WORK_STARTED",
              "approvalId": "",
              "approvalDelta": 0,
              "factionId": "JOSEON_SECTS",
              "factionDelta": 1,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "id": "c1_220__.",
              "text": "약초를 캐자. 초희 잔소리도 조금은 줄겠지.",
              "disposition": 0,
              "targetEntryId": "c1_220",
              "flagAdded": "CH1_VILLAGE_WORK_STARTED",
              "approvalId": "",
              "approvalDelta": 0,
              "factionId": "JOSEON_SECTS",
              "factionDelta": 2,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "id": "c1_230__.",
              "text": "검각부터 고치자. 적보다 비가 먼저 쳐들어온다.",
              "disposition": 2,
              "targetEntryId": "c1_230",
              "flagAdded": "CH1_VILLAGE_WORK_STARTED",
              "approvalId": "",
              "approvalDelta": 0,
              "factionId": "JOSEON_SECTS",
              "factionDelta": 1,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "id": "c1_210",
          "speakerId": "park_sungjun",
          "line": "좋아. 백야검결 제일식, 장작분광. 나무야 미안하다, 우리 집이 가난하다.",
          "mood": "농담",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_220",
          "speakerId": "park_sungjun",
          "line": "산길? 내가 전문이지. 어릴 때 길 잃은 경험이 이렇게 경력이 될 줄이야.",
          "mood": "능청",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_230",
          "speakerId": "park_sungjun",
          "line": "비 새는 검각에서 천하제일을 꿈꾸면 감기부터 천하제일이 된다. 지붕부터 살리자.",
          "mood": "현실적",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_240",
          "speakerId": "",
          "line": "밤이 되자, 성준은 다시 찢어진 깃발 아래 섰다. 낮엔 웃었고, 손에는 물집이 잡혔다.",
          "mood": "서술",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_250",
          "speakerId": "park_sungjun",
          "line": "좋아. 중원인지 뭔지, 우리 집 앞마당까지 들어오면 손님 대접은 못 하지.",
          "mood": "다짐",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "c1_260",
          "speakerId": "",
          "line": "꺼져가던 천광은 아직 남아 있었다. 조금 시끄럽고, 조금 가난하고, 이상하게 포기할 마음은 안 드는 빛이었다.",
          "mood": "서술",
          "backgroundId": "",
          "choices": []
        }
      ],
      "nodes": [
        {
          "nodeId": "chapter1_prologue_001",
          "speakerId": "",
          "speakerName": "",
          "line": "백두산 중턱. 눈은 쌓였고, 검각은 기울었고, 백두천광검문의 깃발은 바람에 ‘나 아직 안 죽었다’는 듯 겨우 펄럭인다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_002",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_002",
          "speakerId": "",
          "speakerName": "",
          "line": "한때는 북방의 명문. 지금은 병든 문주, 무서운 사범, 그리고 지붕 위에서 낮잠 자는 소문주 하나가 전 재산이다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_003",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_003",
          "speakerId": "yeon_ok",
          "speakerName": "연옥",
          "line": "박성준. 내려와라. 지붕은 연무장이 아니고, 네 이불도 아니다.",
          "mood": "엄격",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_004",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_004",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "사범님, 낮잠이 아닙니다. 하늘의 기운을 받아 천광심법을... 음, 누워서 받는 중이었습니다.",
          "mood": "능청",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_005",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_005",
          "speakerId": "yeon_ok",
          "speakerName": "연옥",
          "line": "좋다. 그럼 누운 채로 장작도 패고, 목인도 서른 번 치거라.",
          "mood": "차갑게",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_006",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_006",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "(어떻게 둘러댈까?)",
          "mood": "선택",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_007",
          "choices": [
            {
              "text": "하늘 기운이 아직 덜 내려왔습니다.",
              "disposition": 3,
              "nextNodeId": "chapter1_prologue_007",
              "requiredFlags": [],
              "flagsAdded": [
                "CH1_JOKED_DREAM"
              ],
              "approvalChanges": [],
              "factionChanges": [],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "text": "검도 사람도 휴식이 있어야 빛납니다.",
              "disposition": 1,
              "nextNodeId": "chapter1_prologue_007",
              "requiredFlags": [],
              "flagsAdded": [
                "CH1_JOKED_BLADE_REST"
              ],
              "approvalChanges": [],
              "factionChanges": [],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "text": "사범님 발소리 듣고 이미 마음은 내려갔습니다.",
              "disposition": 0,
              "nextNodeId": "chapter1_prologue_007",
              "requiredFlags": [],
              "flagsAdded": [
                "CH1_JOKED_WAITING"
              ],
              "approvalChanges": [],
              "factionChanges": [],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "nodeId": "chapter1_prologue_007",
          "speakerId": "yeon_ok",
          "speakerName": "연옥",
          "line": "입공만 보면 벌써 천하제일이다. 몸도 내려와라. 문주님이 찾으신다.",
          "mood": "단호",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_008",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_008",
          "speakerId": "",
          "speakerName": "",
          "line": "성준이 뛰어내리자 기와 세 장이 먼저 하산했다. 연옥의 눈썹도 같이 올라갔다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_009",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_009",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "보셨죠? 제 경공이 아니라 검각이 먼저 움직였습니다. 건물이 아주 적극적이에요.",
          "mood": "농담",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_010",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_010",
          "speakerId": "yeon_ok",
          "speakerName": "연옥",
          "line": "다음엔 네 용돈이 먼저 하산할 것이다.",
          "mood": "꾸짖음",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_011",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_011",
          "speakerId": "park_mugyeom",
          "speakerName": "박무겸",
          "line": "성준아. 검은 폼으로 드는 게 아니다. 지켜야 할 것이 있을 때 비로소 손에 붙는다.",
          "mood": "조용함",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_012",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_012",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "아버지, 저는 폼도 지키고 문파도 지키고 밥상도 지키고 싶은데요. 셋 중 밥상이 제일 위태롭습니다.",
          "mood": "능청",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_013",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_013",
          "speakerId": "park_mugyeom",
          "speakerName": "박무겸",
          "line": "그래서 부른 것이다. 중원 문파들이 백두산 영맥을 눈독 들인다는 말이 돈다.",
          "mood": "걱정",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_014",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_014",
          "speakerId": "park_mugyeom",
          "speakerName": "박무겸",
          "line": "천광심법과 백야검결, 이제 네 차례다. 문파가 작아졌다고 이름까지 작게 부르지는 마라.",
          "mood": "당부",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_015",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_015",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "걱정 마세요. 제가 가벼운 건 말투뿐입니다. 건드리면, 꽤 무겁게 받아칠 겁니다.",
          "mood": "다짐",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_016",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_016",
          "speakerId": "",
          "speakerName": "",
          "line": "낮이 되자 성준은 소백촌으로 내려갔다. 마을 사람들은 그를 ‘사고뭉치 도련님’이라 부르면서도, 은근히 길을 터준다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_017",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_017",
          "speakerId": "cho_hui",
          "speakerName": "초희",
          "line": "왔네, 백두산 대표 한량. 오늘은 지붕 말고 땅 밟고 다니네?",
          "mood": "핀잔",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_018",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_018",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "초희야, 그 말투... 나를 기다린 사람의 향기가 난다.",
          "mood": "풍류",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_019",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_019",
          "speakerId": "cho_hui",
          "speakerName": "초희",
          "line": "기다렸지. 장작더미가. 약초 바구니도. 그리고 네가 미룬 외상 장부도.",
          "mood": "현실적",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_020",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_020",
          "speakerId": "",
          "speakerName": "",
          "line": "문파 부흥은 거창한 비급에서 시작되지 않았다. 장작, 약초, 지붕 수리. 듣기엔 초라해도 은전은 거짓말을 하지 않는다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_021",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_021",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "(오늘은 뭘 해야 덜 혼나고 더 벌까?)",
          "mood": "선택",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_022",
          "choices": [
            {
              "text": "장작부터 패자. 백야검결로 나무도 감동시켜보자.",
              "disposition": 1,
              "nextNodeId": "chapter1_prologue_022",
              "requiredFlags": [],
              "flagsAdded": [
                "CH1_VILLAGE_WORK_STARTED"
              ],
              "approvalChanges": [],
              "factionChanges": [
                {
                  "id": "JOSEON_SECTS",
                  "delta": 1
                }
              ],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "text": "약초를 캐자. 초희 잔소리도 조금은 줄겠지.",
              "disposition": 0,
              "nextNodeId": "chapter1_prologue_023",
              "requiredFlags": [],
              "flagsAdded": [
                "CH1_VILLAGE_WORK_STARTED"
              ],
              "approvalChanges": [],
              "factionChanges": [
                {
                  "id": "JOSEON_SECTS",
                  "delta": 2
                }
              ],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "text": "검각부터 고치자. 적보다 비가 먼저 쳐들어온다.",
              "disposition": 2,
              "nextNodeId": "chapter1_prologue_024",
              "requiredFlags": [],
              "flagsAdded": [
                "CH1_VILLAGE_WORK_STARTED"
              ],
              "approvalChanges": [],
              "factionChanges": [
                {
                  "id": "JOSEON_SECTS",
                  "delta": 1
                }
              ],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "nodeId": "chapter1_prologue_022",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "좋아. 백야검결 제일식, 장작분광. 나무야 미안하다, 우리 집이 가난하다.",
          "mood": "농담",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_023",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_023",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "산길? 내가 전문이지. 어릴 때 길 잃은 경험이 이렇게 경력이 될 줄이야.",
          "mood": "능청",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_024",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_024",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "비 새는 검각에서 천하제일을 꿈꾸면 감기부터 천하제일이 된다. 지붕부터 살리자.",
          "mood": "현실적",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_025",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_025",
          "speakerId": "",
          "speakerName": "",
          "line": "밤이 되자, 성준은 다시 찢어진 깃발 아래 섰다. 낮엔 웃었고, 손에는 물집이 잡혔다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_026",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_026",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "좋아. 중원인지 뭔지, 우리 집 앞마당까지 들어오면 손님 대접은 못 하지.",
          "mood": "다짐",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "chapter1_prologue_027",
          "choices": []
        },
        {
          "nodeId": "chapter1_prologue_027",
          "speakerId": "",
          "speakerName": "",
          "line": "꺼져가던 천광은 아직 남아 있었다. 조금 시끄럽고, 조금 가난하고, 이상하게 포기할 마음은 안 드는 빛이었다.",
          "mood": "서술",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "",
          "choices": []
        }
      ]
    },
    {
      "id": "companion_baek_ryeon_talk",
      "title": "백련 첫 대화",
      "location": "백두산 검각",
      "backgroundId": "joseon_murim_game_map",
      "startNodeId": "companion_baek_ryeon_talk_001",
      "entries": [
        {
          "id": "t0",
          "speakerId": "baek_ryeon",
          "line": "“창끝은 차갑게 둘게요. 대신 사람 마음까지 얼리라는 명령은... 조금 곤란합니다.”",
          "mood": "차분",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "t1",
          "speakerId": "park_sungjun",
          "line": "(어떻게 답할까?)",
          "mood": "선택",
          "backgroundId": "",
          "choices": [
            {
              "id": "t2a__.",
              "text": "좋아. 먼저 사람부터 살리자.",
              "disposition": 0,
              "targetEntryId": "t2a",
              "flagAdded": "",
              "approvalId": "baek_ryeon",
              "approvalDelta": 3,
              "factionId": "",
              "factionDelta": 0,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "id": "t2b__.",
              "text": "전열부터 잡자. 그래도 사람은 버리지 않는다.",
              "disposition": 2,
              "targetEntryId": "t2b",
              "flagAdded": "",
              "approvalId": "baek_ryeon",
              "approvalDelta": -2,
              "factionId": "",
              "factionDelta": 0,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "id": "t2a",
          "speakerId": "baek_ryeon",
          "line": "백련이 아주 작게 웃는다. “그 순서라면, 제 창도 덜 차가워질 것 같네요.”",
          "mood": "안도",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "t2b",
          "speakerId": "baek_ryeon",
          "line": "백련이 눈을 내리깐다. “말씀은 차갑지만... 버리지 않겠다는 쪽을 믿겠습니다.”",
          "mood": "서늘",
          "backgroundId": "",
          "choices": []
        }
      ],
      "nodes": [
        {
          "nodeId": "companion_baek_ryeon_talk_001",
          "speakerId": "baek_ryeon",
          "speakerName": "백련",
          "line": "“창끝은 차갑게 둘게요. 대신 사람 마음까지 얼리라는 명령은... 조금 곤란합니다.”",
          "mood": "차분",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "companion_baek_ryeon_talk_002",
          "choices": []
        },
        {
          "nodeId": "companion_baek_ryeon_talk_002",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "(어떻게 답할까?)",
          "mood": "선택",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "companion_baek_ryeon_talk_003",
          "choices": [
            {
              "text": "좋아. 먼저 사람부터 살리자.",
              "disposition": 0,
              "nextNodeId": "companion_baek_ryeon_talk_003",
              "requiredFlags": [],
              "flagsAdded": [],
              "approvalChanges": [
                {
                  "id": "baek_ryeon",
                  "delta": 3
                }
              ],
              "factionChanges": [],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "text": "전열부터 잡자. 그래도 사람은 버리지 않는다.",
              "disposition": 2,
              "nextNodeId": "companion_baek_ryeon_talk_004",
              "requiredFlags": [],
              "flagsAdded": [],
              "approvalChanges": [
                {
                  "id": "baek_ryeon",
                  "delta": -2
                }
              ],
              "factionChanges": [],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "nodeId": "companion_baek_ryeon_talk_003",
          "speakerId": "baek_ryeon",
          "speakerName": "백련",
          "line": "백련이 아주 작게 웃는다. “그 순서라면, 제 창도 덜 차가워질 것 같네요.”",
          "mood": "안도",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "companion_baek_ryeon_talk_004",
          "choices": []
        },
        {
          "nodeId": "companion_baek_ryeon_talk_004",
          "speakerId": "baek_ryeon",
          "speakerName": "백련",
          "line": "백련이 눈을 내리깐다. “말씀은 차갑지만... 버리지 않겠다는 쪽을 믿겠습니다.”",
          "mood": "서늘",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "",
          "choices": []
        }
      ]
    },
    {
      "id": "companion_do_arin_talk",
      "title": "도아린 첫 대화",
      "location": "백두산 검각",
      "backgroundId": "joseon_murim_game_map",
      "startNodeId": "companion_do_arin_talk_001",
      "entries": [
        {
          "id": "t0",
          "speakerId": "do_arin",
          "line": "“문주, 길게 말하면 불 꺼져. 저놈들 오면 내가 앞에서 확 열어버릴게.”",
          "mood": "호쾌",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "t1",
          "speakerId": "park_sungjun",
          "line": "(어떻게 답할까?)",
          "mood": "선택",
          "backgroundId": "",
          "choices": [
            {
              "id": "t2a__.",
              "text": "좋다. 대신 한 발만 앞서라.",
              "disposition": 1,
              "targetEntryId": "t2a",
              "flagAdded": "",
              "approvalId": "do_arin",
              "approvalDelta": 2,
              "factionId": "",
              "factionDelta": 0,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "id": "t2b__.",
              "text": "가라. 오늘 길은 네 불로 낸다.",
              "disposition": 2,
              "targetEntryId": "t2b",
              "flagAdded": "",
              "approvalId": "do_arin",
              "approvalDelta": 4,
              "factionId": "",
              "factionDelta": 0,
              "battleKey": "",
              "battleValue": 0,
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "id": "t2a",
          "speakerId": "do_arin",
          "line": "도아린이 히죽 웃고 도집을 친다. “한 발. 음... 큰 한 발은 괜찮지?”",
          "mood": "씩씩",
          "backgroundId": "",
          "choices": []
        },
        {
          "id": "t2b",
          "speakerId": "do_arin",
          "line": "도아린의 눈이 반짝인다. “그 말, 내가 제일 좋아하는 종류야.”",
          "mood": "전의",
          "backgroundId": "",
          "choices": []
        }
      ],
      "nodes": [
        {
          "nodeId": "companion_do_arin_talk_001",
          "speakerId": "do_arin",
          "speakerName": "도아린",
          "line": "“문주, 길게 말하면 불 꺼져. 저놈들 오면 내가 앞에서 확 열어버릴게.”",
          "mood": "호쾌",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "companion_do_arin_talk_002",
          "choices": []
        },
        {
          "nodeId": "companion_do_arin_talk_002",
          "speakerId": "park_sungjun",
          "speakerName": "박성준",
          "line": "(어떻게 답할까?)",
          "mood": "선택",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "companion_do_arin_talk_003",
          "choices": [
            {
              "text": "좋다. 대신 한 발만 앞서라.",
              "disposition": 1,
              "nextNodeId": "companion_do_arin_talk_003",
              "requiredFlags": [],
              "flagsAdded": [],
              "approvalChanges": [
                {
                  "id": "do_arin",
                  "delta": 2
                }
              ],
              "factionChanges": [],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            },
            {
              "text": "가라. 오늘 길은 네 불로 낸다.",
              "disposition": 2,
              "nextNodeId": "companion_do_arin_talk_004",
              "requiredFlags": [],
              "flagsAdded": [],
              "approvalChanges": [
                {
                  "id": "do_arin",
                  "delta": 4
                }
              ],
              "factionChanges": [],
              "battleModifiers": [],
              "romanticIntent": false,
              "sceneCommand": ""
            }
          ]
        },
        {
          "nodeId": "companion_do_arin_talk_003",
          "speakerId": "do_arin",
          "speakerName": "도아린",
          "line": "도아린이 히죽 웃고 도집을 친다. “한 발. 음... 큰 한 발은 괜찮지?”",
          "mood": "씩씩",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "companion_do_arin_talk_004",
          "choices": []
        },
        {
          "nodeId": "companion_do_arin_talk_004",
          "speakerId": "do_arin",
          "speakerName": "도아린",
          "line": "도아린의 눈이 반짝인다. “그 말, 내가 제일 좋아하는 종류야.”",
          "mood": "전의",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "",
          "choices": []
        }
      ]
    },
    {
      "id": "companion_jin_seoyul_talk",
      "title": "진서율 첫 대화",
      "location": "백두산 검각",
      "backgroundId": "joseon_murim_game_map",
      "startNodeId": "companion_jin_seoyul_talk_001",
      "entries": [
        {
          "id": "t0",
          "speakerId": "jin_seoyul",
          "line": "“문주님, 저 물받이 보이죠? 저기에 번개 한 줄만 흘리면 감찰단이 춤추듯 넘어질걸요. 물론... 이론상요!”",
          "mood": "흥분",
          "backgroundId": "",
          "choices": []
        }
      ],
      "nodes": [
        {
          "nodeId": "companion_jin_seoyul_talk_001",
          "speakerId": "jin_seoyul",
          "speakerName": "진서율",
          "line": "“문주님, 저 물받이 보이죠? 저기에 번개 한 줄만 흘리면 감찰단이 춤추듯 넘어질걸요. 물론... 이론상요!”",
          "mood": "흥분",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "",
          "choices": []
        }
      ]
    },
    {
      "id": "companion_seo_a_talk",
      "title": "신서아 첫 대화",
      "location": "백두산 검각",
      "backgroundId": "joseon_murim_game_map",
      "startNodeId": "companion_seo_a_talk_001",
      "entries": [
        {
          "id": "t0",
          "speakerId": "seo_a",
          "line": "“나도 할 수 있어요! 작다고 얕보면 안 돼요. 꽃바람은 원래 빈틈으로 쏙 들어가는 법이라구요!”",
          "mood": "밝음",
          "backgroundId": "",
          "choices": []
        }
      ],
      "nodes": [
        {
          "nodeId": "companion_seo_a_talk_001",
          "speakerId": "seo_a",
          "speakerName": "신서아",
          "line": "“나도 할 수 있어요! 작다고 얕보면 안 돼요. 꽃바람은 원래 빈틈으로 쏙 들어가는 법이라구요!”",
          "mood": "밝음",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "",
          "choices": []
        }
      ]
    },
    {
      "id": "companion_han_biyeon_talk",
      "title": "한비연 첫 대화",
      "location": "백두산 검각",
      "backgroundId": "joseon_murim_game_map",
      "startNodeId": "companion_han_biyeon_talk_001",
      "entries": [
        {
          "id": "t0",
          "speakerId": "han_biyeon",
          "line": "“정면승부? 멋은 있지. 내 취향은 아니고. 나는 그림자길로 돌아가서, 저쪽 허리부터 톡 건드릴게.”",
          "mood": "낮게",
          "backgroundId": "",
          "choices": []
        }
      ],
      "nodes": [
        {
          "nodeId": "companion_han_biyeon_talk_001",
          "speakerId": "han_biyeon",
          "speakerName": "한비연",
          "line": "“정면승부? 멋은 있지. 내 취향은 아니고. 나는 그림자길로 돌아가서, 저쪽 허리부터 톡 건드릴게.”",
          "mood": "낮게",
          "backgroundId": "joseon_murim_game_map",
          "portraitId": "",
          "portraitResource": "",
          "nextNodeId": "",
          "choices": []
        }
      ]
    }
  ],
  "mapAssets": [
    {
      "id": "map_tile_plain_moss",
      "title": "백두 이끼 평지",
      "category": "terrain",
      "subtype": "Plain",
      "resourcePath": "MapAssets/Tiles/plain_moss",
      "previewUrl": "/resources/MapAssets/Tiles/plain_moss.png",
      "file": "MapAssets/Tiles/plain_moss.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Plain"
      ],
      "notes": "하단 진입로와 완만한 초지용 기본 타일"
    },
    {
      "id": "map_tile_hill_moss",
      "title": "백두 능선 언덕",
      "category": "terrain",
      "subtype": "Hill",
      "resourcePath": "MapAssets/Tiles/hill_moss",
      "previewUrl": "/resources/MapAssets/Tiles/hill_moss.png",
      "file": "MapAssets/Tiles/hill_moss.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Hill"
      ],
      "notes": "고저 차가 보이는 능선/언덕 타일"
    },
    {
      "id": "map_tile_stone_courtyard",
      "title": "폐사당 석정 마당",
      "category": "terrain",
      "subtype": "Stone",
      "resourcePath": "MapAssets/Tiles/stone_courtyard",
      "previewUrl": "/resources/MapAssets/Tiles/stone_courtyard.png",
      "file": "MapAssets/Tiles/stone_courtyard.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Stone"
      ],
      "notes": "중앙 마당과 중립 보행 지형"
    },
    {
      "id": "map_tile_road_stair",
      "title": "중앙 돌계단 길",
      "category": "terrain",
      "subtype": "Road",
      "resourcePath": "MapAssets/Tiles/road_stair",
      "previewUrl": "/resources/MapAssets/Tiles/road_stair.png",
      "file": "MapAssets/Tiles/road_stair.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Road"
      ],
      "notes": "중앙 병목 돌계단/진입로"
    },
    {
      "id": "map_tile_shrine_floor",
      "title": "천광 사당 석단",
      "category": "terrain",
      "subtype": "ShrineFloor",
      "resourcePath": "MapAssets/Tiles/shrine_floor",
      "previewUrl": "/resources/MapAssets/Tiles/shrine_floor.png",
      "file": "MapAssets/Tiles/shrine_floor.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "ShrineFloor"
      ],
      "notes": "목표 지점과 폐사당 고지용 타일"
    },
    {
      "id": "map_tile_bamboo_floor",
      "title": "대숲 그림자 바닥",
      "category": "terrain",
      "subtype": "Bamboo",
      "resourcePath": "MapAssets/Tiles/bamboo_floor",
      "previewUrl": "/resources/MapAssets/Tiles/bamboo_floor.png",
      "file": "MapAssets/Tiles/bamboo_floor.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Bamboo"
      ],
      "notes": "좌측 대나무숲 샛길"
    },
    {
      "id": "map_tile_forest_floor",
      "title": "짙은 산림 바닥",
      "category": "terrain",
      "subtype": "Forest",
      "resourcePath": "MapAssets/Tiles/forest_floor",
      "previewUrl": "/resources/MapAssets/Tiles/forest_floor.png",
      "file": "MapAssets/Tiles/forest_floor.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Forest"
      ],
      "notes": "시야를 가리는 숲 지형"
    },
    {
      "id": "map_tile_shallow_water",
      "title": "압록 얕은 물결",
      "category": "terrain",
      "subtype": "ShallowWater",
      "resourcePath": "MapAssets/Tiles/shallow_water",
      "previewUrl": "/resources/MapAssets/Tiles/shallow_water.png",
      "file": "MapAssets/Tiles/shallow_water.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "ShallowWater"
      ],
      "notes": "이동 부담이 있는 얕은 물"
    },
    {
      "id": "map_tile_deep_water",
      "title": "검푸른 깊은 물",
      "category": "terrain",
      "subtype": "DeepWater",
      "resourcePath": "MapAssets/Tiles/deep_water",
      "previewUrl": "/resources/MapAssets/Tiles/deep_water.png",
      "file": "MapAssets/Tiles/deep_water.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "DeepWater"
      ],
      "notes": "통행 제한이 강한 깊은 물"
    },
    {
      "id": "map_tile_wood_plank",
      "title": "낡은 목판 바닥",
      "category": "terrain",
      "subtype": "Wood",
      "resourcePath": "MapAssets/Tiles/wood_plank",
      "previewUrl": "/resources/MapAssets/Tiles/wood_plank.png",
      "file": "MapAssets/Tiles/wood_plank.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Wood"
      ],
      "notes": "누각/목재 지형"
    },
    {
      "id": "map_tile_wood_bridge",
      "title": "낡은 나무다리",
      "category": "terrain",
      "subtype": "Bridge",
      "resourcePath": "MapAssets/Tiles/wood_bridge",
      "previewUrl": "/resources/MapAssets/Tiles/wood_bridge.png",
      "file": "MapAssets/Tiles/wood_bridge.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Bridge"
      ],
      "notes": "끊거나 우회할 수 있는 다리"
    },
    {
      "id": "map_tile_roof_tile",
      "title": "붉은 기와지붕",
      "category": "terrain",
      "subtype": "Roof",
      "resourcePath": "MapAssets/Tiles/roof_tile",
      "previewUrl": "/resources/MapAssets/Tiles/roof_tile.png",
      "file": "MapAssets/Tiles/roof_tile.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Roof"
      ],
      "notes": "높은 지붕/누각 고지"
    },
    {
      "id": "map_tile_cliff_face",
      "title": "검은 절벽면",
      "category": "terrain",
      "subtype": "Cliff",
      "resourcePath": "MapAssets/Tiles/cliff_face",
      "previewUrl": "/resources/MapAssets/Tiles/cliff_face.png",
      "file": "MapAssets/Tiles/cliff_face.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Cliff"
      ],
      "notes": "통행 불가 절벽/시야 차단"
    },
    {
      "id": "map_tile_wall_broken",
      "title": "무너진 담장",
      "category": "terrain",
      "subtype": "Wall",
      "resourcePath": "MapAssets/Tiles/wall_broken",
      "previewUrl": "/resources/MapAssets/Tiles/wall_broken.png",
      "file": "MapAssets/Tiles/wall_broken.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Wall"
      ],
      "notes": "폐사당 담장과 높은 벽"
    },
    {
      "id": "map_tile_rubble",
      "title": "흩어진 잔해",
      "category": "terrain",
      "subtype": "Rubble",
      "resourcePath": "MapAssets/Tiles/rubble",
      "previewUrl": "/resources/MapAssets/Tiles/rubble.png",
      "file": "MapAssets/Tiles/rubble.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Rubble"
      ],
      "notes": "엄폐/이동 부담을 주는 잔해"
    },
    {
      "id": "map_tile_mud_path",
      "title": "질척한 산길",
      "category": "terrain",
      "subtype": "Mud",
      "resourcePath": "MapAssets/Tiles/mud_path",
      "previewUrl": "/resources/MapAssets/Tiles/mud_path.png",
      "file": "MapAssets/Tiles/mud_path.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Mud"
      ],
      "notes": "비 온 뒤 산길/속도 저하 지형"
    },
    {
      "id": "map_tile_snow_edge",
      "title": "백두 잔설 바닥",
      "category": "terrain",
      "subtype": "Snow",
      "resourcePath": "MapAssets/Tiles/snow_edge",
      "previewUrl": "/resources/MapAssets/Tiles/snow_edge.png",
      "file": "MapAssets/Tiles/snow_edge.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Snow"
      ],
      "notes": "백두산 잔설 지형"
    },
    {
      "id": "map_tile_ice_slick",
      "title": "서리 낀 얼음판",
      "category": "terrain",
      "subtype": "Ice",
      "resourcePath": "MapAssets/Tiles/ice_slick",
      "previewUrl": "/resources/MapAssets/Tiles/ice_slick.png",
      "file": "MapAssets/Tiles/ice_slick.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Ice"
      ],
      "notes": "서리/빙결 전술 지형"
    },
    {
      "id": "map_tile_gate_threshold",
      "title": "문루 문턱",
      "category": "terrain",
      "subtype": "Gate",
      "resourcePath": "MapAssets/Tiles/gate_threshold",
      "previewUrl": "/resources/MapAssets/Tiles/gate_threshold.png",
      "file": "MapAssets/Tiles/gate_threshold.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Gate"
      ],
      "notes": "문/입구/봉쇄 지형"
    },
    {
      "id": "map_tile_fire_scorch",
      "title": "불길 그을림",
      "category": "terrain",
      "subtype": "Fire",
      "resourcePath": "MapAssets/Tiles/fire_scorch",
      "previewUrl": "/resources/MapAssets/Tiles/fire_scorch.png",
      "file": "MapAssets/Tiles/fire_scorch.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Fire"
      ],
      "notes": "화염이 남은 위험 지형"
    },
    {
      "id": "map_tile_smoke_veil",
      "title": "연무 낀 바닥",
      "category": "terrain",
      "subtype": "Smoke",
      "resourcePath": "MapAssets/Tiles/smoke_veil",
      "previewUrl": "/resources/MapAssets/Tiles/smoke_veil.png",
      "file": "MapAssets/Tiles/smoke_veil.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Smoke"
      ],
      "notes": "시야 차단 연무 지형"
    },
    {
      "id": "map_tile_trap_mark",
      "title": "암기 함정 표식",
      "category": "terrain",
      "subtype": "Trap",
      "resourcePath": "MapAssets/Tiles/trap_mark",
      "previewUrl": "/resources/MapAssets/Tiles/trap_mark.png",
      "file": "MapAssets/Tiles/trap_mark.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Trap"
      ],
      "notes": "함정/위험 표시 타일"
    },
    {
      "id": "map_object_sect_signboard",
      "title": "백두천광 현판",
      "category": "object",
      "subtype": "SectSignboard",
      "resourcePath": "MapAssets/Objects/sect_signboard",
      "previewUrl": "/resources/MapAssets/Objects/sect_signboard.png",
      "file": "MapAssets/Objects/sect_signboard.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "SectSignboard"
      ],
      "notes": "보호 목표 오브젝트"
    },
    {
      "id": "map_object_incense_burner",
      "title": "제단 향로",
      "category": "object",
      "subtype": "IncenseBurner",
      "resourcePath": "MapAssets/Objects/incense_burner",
      "previewUrl": "/resources/MapAssets/Objects/incense_burner.png",
      "file": "MapAssets/Objects/incense_burner.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "IncenseBurner"
      ],
      "notes": "연막/기도 연출 오브젝트"
    },
    {
      "id": "map_object_red_lantern",
      "title": "붉은 등불",
      "category": "object",
      "subtype": "Lantern",
      "resourcePath": "MapAssets/Objects/red_lantern",
      "previewUrl": "/resources/MapAssets/Objects/red_lantern.png",
      "file": "MapAssets/Objects/red_lantern.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "Lantern"
      ],
      "notes": "화염 상호작용 오브젝트"
    },
    {
      "id": "map_object_oil_jar",
      "title": "기름항아리",
      "category": "object",
      "subtype": "OilJar",
      "resourcePath": "MapAssets/Objects/oil_jar",
      "previewUrl": "/resources/MapAssets/Objects/oil_jar.png",
      "file": "MapAssets/Objects/oil_jar.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "OilJar"
      ],
      "notes": "폭발/화염 전술 오브젝트"
    },
    {
      "id": "map_object_wine_cart",
      "title": "술수레",
      "category": "object",
      "subtype": "WineCart",
      "resourcePath": "MapAssets/Objects/wine_cart",
      "previewUrl": "/resources/MapAssets/Objects/wine_cart.png",
      "file": "MapAssets/Objects/wine_cart.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "WineCart"
      ],
      "notes": "이동 엄폐 오브젝트"
    },
    {
      "id": "map_object_fallen_wall",
      "title": "무너진 담장 조각",
      "category": "object",
      "subtype": "FallenWall",
      "resourcePath": "MapAssets/Objects/fallen_wall",
      "previewUrl": "/resources/MapAssets/Objects/fallen_wall.png",
      "file": "MapAssets/Objects/fallen_wall.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "FallenWall"
      ],
      "notes": "엄폐/시야 차단 오브젝트"
    },
    {
      "id": "map_object_bridge_rope",
      "title": "낡은 다리 밧줄",
      "category": "object",
      "subtype": "BridgeRope",
      "resourcePath": "MapAssets/Objects/bridge_rope",
      "previewUrl": "/resources/MapAssets/Objects/bridge_rope.png",
      "file": "MapAssets/Objects/bridge_rope.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "BridgeRope"
      ],
      "notes": "다리 붕괴 상호작용 오브젝트"
    },
    {
      "id": "map_object_bamboo_bundle",
      "title": "대나무 묶음",
      "category": "object",
      "subtype": "BambooBundle",
      "resourcePath": "MapAssets/Objects/bamboo_bundle",
      "previewUrl": "/resources/MapAssets/Objects/bamboo_bundle.png",
      "file": "MapAssets/Objects/bamboo_bundle.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "BambooBundle"
      ],
      "notes": "넘겨서 길목을 막는 오브젝트"
    },
    {
      "id": "map_object_stone_lantern",
      "title": "석등",
      "category": "object",
      "subtype": "RockLantern",
      "resourcePath": "MapAssets/Objects/stone_lantern",
      "previewUrl": "/resources/MapAssets/Objects/stone_lantern.png",
      "file": "MapAssets/Objects/stone_lantern.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "RockLantern"
      ],
      "notes": "낙석/파괴 연출 오브젝트"
    },
    {
      "id": "map_object_falling_boulder",
      "title": "낙석 바위",
      "category": "object",
      "subtype": "Rockfall",
      "resourcePath": "MapAssets/Objects/falling_boulder",
      "previewUrl": "/resources/MapAssets/Objects/falling_boulder.png",
      "file": "MapAssets/Objects/falling_boulder.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "Rockfall"
      ],
      "notes": "낙석 위험 표시"
    },
    {
      "id": "map_object_flame_pillar",
      "title": "솟는 화염",
      "category": "object",
      "subtype": "Fire",
      "resourcePath": "MapAssets/Objects/flame_pillar",
      "previewUrl": "/resources/MapAssets/Objects/flame_pillar.png",
      "file": "MapAssets/Objects/flame_pillar.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "Fire"
      ],
      "notes": "화염 상태 연출"
    },
    {
      "id": "map_object_smoke_wisp",
      "title": "흩어지는 연무",
      "category": "object",
      "subtype": "Smoke",
      "resourcePath": "MapAssets/Objects/smoke_wisp",
      "previewUrl": "/resources/MapAssets/Objects/smoke_wisp.png",
      "file": "MapAssets/Objects/smoke_wisp.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "Smoke"
      ],
      "notes": "연막 상태 연출"
    },
    {
      "id": "map_tile_baekdu_snow_plain",
      "title": "백두 설원 평지",
      "category": "terrain",
      "subtype": "Snow",
      "resourcePath": "MapAssets/Tiles/baekdu_snow_plain",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_snow_plain.png",
      "file": "MapAssets/Tiles/baekdu_snow_plain.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Snow"
      ],
      "notes": "백두산 설산 기본 설원 타일"
    },
    {
      "id": "map_tile_baekdu_deep_snow",
      "title": "백두 깊은 눈더미",
      "category": "terrain",
      "subtype": "DeepSnow",
      "resourcePath": "MapAssets/Tiles/baekdu_deep_snow",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_deep_snow.png",
      "file": "MapAssets/Tiles/baekdu_deep_snow.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "DeepSnow"
      ],
      "notes": "이동을 늦추는 깊은 적설 지형"
    },
    {
      "id": "map_tile_baekdu_wind_snow_ridge",
      "title": "바람깎인 설릉",
      "category": "terrain",
      "subtype": "SnowRidge",
      "resourcePath": "MapAssets/Tiles/baekdu_wind_snow_ridge",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_wind_snow_ridge.png",
      "file": "MapAssets/Tiles/baekdu_wind_snow_ridge.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "SnowRidge"
      ],
      "notes": "고저차가 보이는 설산 능선"
    },
    {
      "id": "map_tile_baekdu_ice_slick",
      "title": "백두 빙판",
      "category": "terrain",
      "subtype": "Ice",
      "resourcePath": "MapAssets/Tiles/baekdu_ice_slick",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_ice_slick.png",
      "file": "MapAssets/Tiles/baekdu_ice_slick.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "Ice"
      ],
      "notes": "미끄러짐/빙결 전술용 얼음판"
    },
    {
      "id": "map_tile_baekdu_frozen_stream",
      "title": "얼어붙은 얕은 계류",
      "category": "terrain",
      "subtype": "FrozenStream",
      "resourcePath": "MapAssets/Tiles/baekdu_frozen_stream",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_frozen_stream.png",
      "file": "MapAssets/Tiles/baekdu_frozen_stream.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "FrozenStream"
      ],
      "notes": "얕은 물이 얼어붙은 지형"
    },
    {
      "id": "map_tile_baekdu_dark_frozen_water",
      "title": "검푸른 결빙수",
      "category": "terrain",
      "subtype": "FrozenDeepWater",
      "resourcePath": "MapAssets/Tiles/baekdu_dark_frozen_water",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_dark_frozen_water.png",
      "file": "MapAssets/Tiles/baekdu_dark_frozen_water.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "FrozenDeepWater"
      ],
      "notes": "깊은 물이 얼어붙은 위험 지형"
    },
    {
      "id": "map_tile_baekdu_volcanic_snow_rock",
      "title": "눈 덮인 화산암",
      "category": "terrain",
      "subtype": "VolcanicRock",
      "resourcePath": "MapAssets/Tiles/baekdu_volcanic_snow_rock",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_volcanic_snow_rock.png",
      "file": "MapAssets/Tiles/baekdu_volcanic_snow_rock.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "VolcanicRock"
      ],
      "notes": "백두 화산암과 눈이 섞인 지형"
    },
    {
      "id": "map_tile_baekdu_snow_basalt_cliff",
      "title": "설산 현무암 절벽",
      "category": "terrain",
      "subtype": "BasaltCliff",
      "resourcePath": "MapAssets/Tiles/baekdu_snow_basalt_cliff",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_snow_basalt_cliff.png",
      "file": "MapAssets/Tiles/baekdu_snow_basalt_cliff.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "BasaltCliff"
      ],
      "notes": "통행 불가/시야 차단 절벽"
    },
    {
      "id": "map_tile_baekdu_snow_stone_courtyard",
      "title": "눈 덮인 석정",
      "category": "terrain",
      "subtype": "SnowStone",
      "resourcePath": "MapAssets/Tiles/baekdu_snow_stone_courtyard",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_snow_stone_courtyard.png",
      "file": "MapAssets/Tiles/baekdu_snow_stone_courtyard.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "SnowStone"
      ],
      "notes": "사당 주변 석정 설원"
    },
    {
      "id": "map_tile_baekdu_snow_shrine_floor",
      "title": "백두 설사당 석단",
      "category": "terrain",
      "subtype": "SnowShrineFloor",
      "resourcePath": "MapAssets/Tiles/baekdu_snow_shrine_floor",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_snow_shrine_floor.png",
      "file": "MapAssets/Tiles/baekdu_snow_shrine_floor.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "SnowShrineFloor"
      ],
      "notes": "금문양이 희미한 설산 사당 바닥"
    },
    {
      "id": "map_tile_baekdu_frozen_stair_road",
      "title": "얼어붙은 돌계단",
      "category": "terrain",
      "subtype": "FrozenRoad",
      "resourcePath": "MapAssets/Tiles/baekdu_frozen_stair_road",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_frozen_stair_road.png",
      "file": "MapAssets/Tiles/baekdu_frozen_stair_road.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "FrozenRoad"
      ],
      "notes": "빙결된 계단형 병목 지형"
    },
    {
      "id": "map_tile_baekdu_snow_mountain_pass",
      "title": "설산 고갯길",
      "category": "terrain",
      "subtype": "SnowPass",
      "resourcePath": "MapAssets/Tiles/baekdu_snow_mountain_pass",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_snow_mountain_pass.png",
      "file": "MapAssets/Tiles/baekdu_snow_mountain_pass.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "SnowPass"
      ],
      "notes": "갈색 흙길과 눈이 섞인 산길"
    },
    {
      "id": "map_tile_baekdu_snow_bamboo_floor",
      "title": "눈 대숲 바닥",
      "category": "terrain",
      "subtype": "SnowBamboo",
      "resourcePath": "MapAssets/Tiles/baekdu_snow_bamboo_floor",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_snow_bamboo_floor.png",
      "file": "MapAssets/Tiles/baekdu_snow_bamboo_floor.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "SnowBamboo"
      ],
      "notes": "눈 덮인 대나무숲 지형"
    },
    {
      "id": "map_tile_baekdu_snow_pine_floor",
      "title": "눈 소나무숲 바닥",
      "category": "terrain",
      "subtype": "SnowPine",
      "resourcePath": "MapAssets/Tiles/baekdu_snow_pine_floor",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_snow_pine_floor.png",
      "file": "MapAssets/Tiles/baekdu_snow_pine_floor.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "SnowPine"
      ],
      "notes": "소나무와 눈이 섞인 숲 지형"
    },
    {
      "id": "map_tile_baekdu_hot_spring_ground",
      "title": "백두 온천 김 바닥",
      "category": "terrain",
      "subtype": "HotSpring",
      "resourcePath": "MapAssets/Tiles/baekdu_hot_spring_ground",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_hot_spring_ground.png",
      "file": "MapAssets/Tiles/baekdu_hot_spring_ground.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "HotSpring"
      ],
      "notes": "눈 속 온천과 증기 지형"
    },
    {
      "id": "map_tile_baekdu_cracked_ice_hazard",
      "title": "갈라진 빙하 함정",
      "category": "terrain",
      "subtype": "CrackedIce",
      "resourcePath": "MapAssets/Tiles/baekdu_cracked_ice_hazard",
      "previewUrl": "/resources/MapAssets/Tiles/baekdu_cracked_ice_hazard.png",
      "file": "MapAssets/Tiles/baekdu_cracked_ice_hazard.png",
      "tags": [
        "MAP",
        "tile",
        "murim",
        "CrackedIce"
      ],
      "notes": "파괴/추락 위험이 있는 얼음 지형"
    },
    {
      "id": "map_object_baekdu_snow_pine",
      "title": "눈 덮인 소나무",
      "category": "object",
      "subtype": "SnowPine",
      "resourcePath": "MapAssets/Objects/baekdu_snow_pine",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_snow_pine.png",
      "file": "MapAssets/Objects/baekdu_snow_pine.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "SnowPine"
      ],
      "notes": "설산 숲/시야 차단 소품"
    },
    {
      "id": "map_object_baekdu_snowdrift_cover",
      "title": "큰 눈더미 엄폐",
      "category": "object",
      "subtype": "SnowCover",
      "resourcePath": "MapAssets/Objects/baekdu_snowdrift_cover",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_snowdrift_cover.png",
      "file": "MapAssets/Objects/baekdu_snowdrift_cover.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "SnowCover"
      ],
      "notes": "설산 전투용 엄폐 오브젝트"
    },
    {
      "id": "map_object_baekdu_ice_crystal",
      "title": "푸른 빙정 군락",
      "category": "object",
      "subtype": "IceCrystal",
      "resourcePath": "MapAssets/Objects/baekdu_ice_crystal",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_ice_crystal.png",
      "file": "MapAssets/Objects/baekdu_ice_crystal.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "IceCrystal"
      ],
      "notes": "빙결 위험/시야 포인트 오브젝트"
    },
    {
      "id": "map_object_baekdu_frozen_stone_lantern",
      "title": "얼어붙은 석등",
      "category": "object",
      "subtype": "FrozenLantern",
      "resourcePath": "MapAssets/Objects/baekdu_frozen_stone_lantern",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_frozen_stone_lantern.png",
      "file": "MapAssets/Objects/baekdu_frozen_stone_lantern.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "FrozenLantern"
      ],
      "notes": "설산 사당 조명 오브젝트"
    },
    {
      "id": "map_object_baekdu_broken_snow_gate",
      "title": "무너진 설문",
      "category": "object",
      "subtype": "SnowGate",
      "resourcePath": "MapAssets/Objects/baekdu_broken_snow_gate",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_broken_snow_gate.png",
      "file": "MapAssets/Objects/baekdu_broken_snow_gate.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "SnowGate"
      ],
      "notes": "눈 덮인 사당 문루 잔해"
    },
    {
      "id": "map_object_baekdu_frozen_rope_posts",
      "title": "얼어붙은 밧줄 말뚝",
      "category": "object",
      "subtype": "FrozenRope",
      "resourcePath": "MapAssets/Objects/baekdu_frozen_rope_posts",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_frozen_rope_posts.png",
      "file": "MapAssets/Objects/baekdu_frozen_rope_posts.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "FrozenRope"
      ],
      "notes": "빙결된 다리/길목 오브젝트"
    },
    {
      "id": "map_object_baekdu_snow_boulder",
      "title": "눈 덮인 화산 바위",
      "category": "object",
      "subtype": "SnowBoulder",
      "resourcePath": "MapAssets/Objects/baekdu_snow_boulder",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_snow_boulder.png",
      "file": "MapAssets/Objects/baekdu_snow_boulder.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "SnowBoulder"
      ],
      "notes": "낙석/엄폐 겸용 바위"
    },
    {
      "id": "map_object_baekdu_hot_spring_steam",
      "title": "온천 증기",
      "category": "object",
      "subtype": "HotSpringSteam",
      "resourcePath": "MapAssets/Objects/baekdu_hot_spring_steam",
      "previewUrl": "/resources/MapAssets/Objects/baekdu_hot_spring_steam.png",
      "file": "MapAssets/Objects/baekdu_hot_spring_steam.png",
      "tags": [
        "MAP",
        "object",
        "murim",
        "HotSpringSteam"
      ],
      "notes": "시야를 흐리는 김/연무 효과"
    }
  ]
}
;
