using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class SetupQuiz5Scene
{
    private const string Quiz5Path = "Assets/Scenes/quiz_5.unity";
    private const string TemplatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";

    [InitializeOnLoadMethod]
    private static void AutoRunOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Quiz5Path))
            {
                Debug.Log("[Quiz 5] Cena quiz_5.unity não encontrada. Criando automaticamente...");
                BuildScene();
            }
        };
    }

    [MenuItem("Quizzes/Build Quiz 5 Scene")]
    public static void BuildScene()
    {
        UnityEngine.SceneManagement.Scene scene;

        if (File.Exists(TemplatePath))
        {
            scene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        // 1. Configurar e carregar os sprites do pacote Kenney
        string charPath = "Assets/AssetPacks/Kenney_PixelPlatformer/Tiles/Characters/tile_0000.png";
        string groundPath = "Assets/AssetPacks/Kenney_PixelPlatformer/Tiles/tile_0021.png";
        string platformPath = "Assets/AssetPacks/Kenney_PixelPlatformer/Tiles/tile_0009.png";

        ConfigureSpriteImporter(charPath);
        ConfigureSpriteImporter(groundPath);
        if (File.Exists(platformPath))
        {
            ConfigureSpriteImporter(platformPath);
        }
        AssetDatabase.Refresh();

        Sprite charSprite = AssetDatabase.LoadAssetAtPath<Sprite>(charPath);
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundPath);
        Sprite platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(platformPath);
        if (platformSprite == null)
        {
            platformSprite = groundSprite;
        }

        // 2. Criar Personagem (Player)
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(-5f, 1f, 0f);

        SpriteRenderer playerSr = player.AddComponent<SpriteRenderer>();
        playerSr.sprite = charSprite;
        playerSr.sortingOrder = 5;

        Rigidbody2D playerRb = player.AddComponent<Rigidbody2D>();
        playerRb.bodyType = RigidbodyType2D.Dynamic;
        playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerRb.gravityScale = 2.5f;

        BoxCollider2D playerCol = player.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(0.85f, 0.95f);

        PlayerMovement movement = player.AddComponent<PlayerMovement>();

        // Objeto filho GroundCheck posicionado na sola dos pés
        GameObject groundCheckObj = new GameObject("GroundCheck");
        groundCheckObj.transform.parent = player.transform;
        groundCheckObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        SerializedObject movementSo = new SerializedObject(movement);
        SerializedProperty gcProp = movementSo.FindProperty("groundCheck");
        if (gcProp != null)
        {
            gcProp.objectReferenceValue = groundCheckObj.transform;
        }
        SerializedProperty jfProp = movementSo.FindProperty("jumpForce");
        if (jfProp != null)
        {
            jfProp.floatValue = 11f;
        }
        movementSo.ApplyModifiedProperties();

        // 3. Criar Ambiente
        GameObject env = new GameObject("Environment");

        // Plataforma inicial (Lado Esquerdo - onde o player começa)
        CreateStaticPlatform(env.transform, groundSprite, new Vector3(-6f, -1.5f, 0f), new Vector3(5f, 1f, 1f), "Platform_Left_Start");

        // Plataforma Móvel e Queda (Quiz 5)
        // Posicionada logo à frente da plataforma inicial
        GameObject movingPlatformObj = new GameObject("MovingFallingPlatform");
        movingPlatformObj.transform.parent = env.transform;
        movingPlatformObj.transform.position = new Vector3(-1f, -1.5f, 0f);
        movingPlatformObj.transform.localScale = new Vector3(3f, 0.8f, 1f);

        SpriteRenderer movingSr = movingPlatformObj.AddComponent<SpriteRenderer>();
        movingSr.sprite = platformSprite;
        movingSr.sortingOrder = 4;
        movingSr.color = new Color(1f, 0.92f, 0.75f, 1f); // Destaque visual sutil

        BoxCollider2D movingCol = movingPlatformObj.AddComponent<BoxCollider2D>();

        // Requisito 1:
        // Configurada com collider e Rigidbody2D para permanecer parada inicialmente, sem gravidade
        Rigidbody2D movingRb = movingPlatformObj.AddComponent<Rigidbody2D>();
        movingRb.bodyType = RigidbodyType2D.Kinematic;
        movingRb.gravityScale = 0f;
        movingRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        movingRb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Adiciona o script MovingFallingPlatform configurado com os parâmetros requeridos
        MovingFallingPlatform mfp = movingPlatformObj.AddComponent<MovingFallingPlatform>();
        SerializedObject mfpSo = new SerializedObject(mfp);
        SerializedProperty dirProp = mfpSo.FindProperty("moveDirection");
        if (dirProp != null) dirProp.vector2Value = Vector2.right;

        SerializedProperty speedProp = mfpSo.FindProperty("moveSpeed");
        if (speedProp != null) speedProp.floatValue = 3f;

        SerializedProperty durProp = mfpSo.FindProperty("moveDuration");
        if (durProp != null) durProp.floatValue = 5.0f;

        SerializedProperty gravProp = mfpSo.FindProperty("fallGravityScale");
        if (gravProp != null) gravProp.floatValue = 2.5f;

        SerializedProperty tagProp = mfpSo.FindProperty("playerTag");
        if (tagProp != null) tagProp.stringValue = "Player";

        mfpSo.ApplyModifiedProperties();

        // Plataforma de Destino (Lado Direito - final do percurso de 15m)
        CreateStaticPlatform(env.transform, groundSprite, new Vector3(17f, -1.5f, 0f), new Vector3(5f, 1f, 1f), "Platform_Right_Goal");

        // Chão do fosso (onde a plataforma cai ao final dos 5 segundos)
        CreateStaticPlatform(env.transform, groundSprite, new Vector3(6f, -7f, 0f), new Vector3(28f, 1f, 1f), "Pit_Floor");

        // 4. Ajustar Câmera
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(5.5f, 0.5f, -10f);
            cam.orthographicSize = 6.5f;
        }

        // 5. Salvar a cena quiz_5.unity
        EditorSceneManager.SaveScene(scene, Quiz5Path);

        // 6. Registrar em EditorBuildSettings
        RegisterSceneInBuildSettings(Quiz5Path);

        AssetDatabase.SaveAssets();
        Debug.Log("[Quiz 5] Cena criada e salva com sucesso em: " + Quiz5Path);
    }

    private static void ConfigureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 18;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void CreateStaticPlatform(Transform parent, Sprite sprite, Vector3 position, Vector3 scale, string name)
    {
        GameObject plat = new GameObject(name);
        plat.transform.parent = parent;
        plat.transform.position = position;
        plat.transform.localScale = scale;

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        plat.AddComponent<BoxCollider2D>();
    }

    private static void RegisterSceneInBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(newScenes, 0);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
    }
}
