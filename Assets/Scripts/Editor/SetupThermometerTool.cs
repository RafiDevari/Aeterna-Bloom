#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetupThermometerTool : EditorWindow
{
    [MenuItem("Tools/Setup Room Thermometers")]
    public static void Setup()
    {
        // 1. Cari Canvas di Scene
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SetupThermometerTool] Canvas tidak ditemukan di scene!");
            EditorUtility.DisplayDialog("Error", "Canvas tidak ditemukan di scene! Pastikan scene memiliki Canvas UI.", "OK");
            return;
        }

        // 2. Cari atau Tambahkan komponen ThermometerPopup (dimanapun ia berada di scene)
        ThermometerPopup popup = GameObject.FindObjectOfType<ThermometerPopup>(true);
        if (popup == null)
        {
            // Jika tidak ada, buat di GameObject baru di dalam Canvas -> Popup (jika ada) atau Canvas
            Transform popupManager = canvas.transform.Find("Popup") ?? canvas.transform;
            GameObject popupObj = new GameObject("ThermometerPopup");
            popupObj.transform.SetParent(popupManager, false);
            popup = popupObj.AddComponent<ThermometerPopup>();
            Undo.RegisterCreatedObjectUndo(popupObj, "Add ThermometerPopup component");
        }

        // 3. Buat ThermometerPopupRoot sebagai root visual yang di-togle aktif/nonaktif
        // Kita taruh root ini sebagai child dari GameObject yang memegang script ThermometerPopup
        Transform rootT = popup.transform.Find("ThermometerPopupRoot");
        GameObject rootGo;
        if (rootT == null)
        {
            rootGo = new GameObject("ThermometerPopupRoot");
            rootGo.transform.SetParent(popup.transform, false);
            RectTransform rt = rootGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(rootGo, "Create ThermometerPopupRoot");
        }
        else
        {
            rootGo = rootT.gameObject;
        }

        // Overlay Button
        Transform overlayT = rootGo.transform.Find("OverlayButton");
        GameObject overlayGo;
        Button overlayBtn;
        if (overlayT == null)
        {
            overlayGo = new GameObject("OverlayButton");
            overlayGo.transform.SetParent(rootGo.transform, false);
            RectTransform overlayRt = overlayGo.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            
            Image overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f); // Hitam semi-transparan

            overlayBtn = overlayGo.AddComponent<Button>();
        }
        else
        {
            overlayGo = overlayT.gameObject;
            overlayBtn = overlayGo.GetComponent<Button>();
        }

        // Panel Box
        Transform panelT = rootGo.transform.Find("Panel");
        GameObject panelGo;
        if (panelT == null)
        {
            panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(rootGo.transform, false);
            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(200f, 260f);
            panelRt.anchoredPosition = Vector2.zero;

            Image panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f); // Warna dark premium

            // Opsional: Tambah Outline/Border agar terlihat rapi
            Outline outline = panelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.8f, 1f, 0.5f);
            outline.effectDistance = new Vector2(2f, 2f);
        }
        else
        {
            panelGo = panelT.gameObject;
        }

        // Room Name Text
        Transform nameT = panelGo.transform.Find("RoomNameText");
        TextMeshProUGUI nameText;
        if (nameT == null)
        {
            GameObject go = new GameObject("RoomNameText");
            go.transform.SetParent(panelGo.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-20f, 40f);
            rt.anchoredPosition = new Vector2(0f, -15f);

            nameText = go.AddComponent<TextMeshProUGUI>();
            nameText.text = "Nama Ruangan";
            nameText.fontSize = 16f;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.fontStyle = FontStyles.Bold;
        }
        else
        {
            nameText = nameT.GetComponent<TextMeshProUGUI>();
        }

        // Temperature Text (Display suhu utama)
        Transform tempT = panelGo.transform.Find("TemperatureText");
        TextMeshProUGUI tempText;
        if (tempT == null)
        {
            GameObject go = new GameObject("TemperatureText");
            go.transform.SetParent(panelGo.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 60f);
            rt.anchoredPosition = new Vector2(0f, 35f);

            tempText = go.AddComponent<TextMeshProUGUI>();
            tempText.text = "20°";
            tempText.fontSize = 44f;
            tempText.alignment = TextAlignmentOptions.Center;
            tempText.color = new Color(0.3f, 0.8f, 1f); // Warna biru termometer
            tempText.fontStyle = FontStyles.Bold;
        }
        else
        {
            tempText = tempT.GetComponent<TextMeshProUGUI>();
        }

        // Up Button (▲)
        Transform upT = panelGo.transform.Find("UpButton");
        Button upBtn;
        if (upT == null)
        {
            GameObject go = new GameObject("UpButton");
            go.transform.SetParent(panelGo.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 35f);
            rt.anchoredPosition = new Vector2(0f, -25f);

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            upBtn = go.AddComponent<Button>();

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI t = textGo.AddComponent<TextMeshProUGUI>();
            t.text = "▲";
            t.fontSize = 18f;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
        }
        else
        {
            upBtn = upT.GetComponent<Button>();
        }

        // Down Button (▼)
        Transform downT = panelGo.transform.Find("DownButton");
        Button downBtn;
        if (downT == null)
        {
            GameObject go = new GameObject("DownButton");
            go.transform.SetParent(panelGo.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 35f);
            rt.anchoredPosition = new Vector2(0f, -70f);

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            downBtn = go.AddComponent<Button>();

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI t = textGo.AddComponent<TextMeshProUGUI>();
            t.text = "▼";
            t.fontSize = 18f;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
        }
        else
        {
            downBtn = downT.GetComponent<Button>();
        }

        // Gunakan SerializedObject agar perubahan field Inspector benar-benar tersimpan!
        SerializedObject popupSO = new SerializedObject(popup);
        
        // PopupBase fields
        popupSO.FindProperty("popupRoot").objectReferenceValue = rootGo;
        popupSO.FindProperty("overlayButton").objectReferenceValue = overlayBtn;
        
        // ThermometerPopup fields
        popupSO.FindProperty("roomNameText").objectReferenceValue = nameText;
        popupSO.FindProperty("temperatureText").objectReferenceValue = tempText;
        popupSO.FindProperty("upButton").objectReferenceValue = upBtn;
        popupSO.FindProperty("downButton").objectReferenceValue = downBtn;
        
        popupSO.ApplyModifiedProperties();

        // Pastikan GameObject ThermometerPopup-nya selalu AKTIF, tapi rootGo-nya MATI
        popup.gameObject.SetActive(true);
        rootGo.SetActive(false);

        // 4. Cari semua Room di scene dan buat tombol termometer
        Room[] rooms = GameObject.FindObjectsOfType<Room>();
        int roomSetupCount = 0;

        foreach (Room room in rooms)
        {
            // Reset nilai 'temperature' di Room agar tidak stuck di 0 akibat serialization lama
            SerializedObject roomSO = new SerializedObject(room);
            var tempProp = roomSO.FindProperty("temperature");
            if (tempProp != null)
            {
                tempProp.floatValue = 20f;
                roomSO.ApplyModifiedProperties();
            }

            Transform thermT = room.transform.Find("ThermometerButton");
            if (thermT != null)
            {
                // Hapus yang lama agar bisa direbuild dengan pengaturan terbaru
                Undo.DestroyObjectImmediate(thermT.gameObject);
            }

            GameObject thermGo = new GameObject("ThermometerButton");
            thermGo.transform.SetParent(room.transform, false);
            
            // localPosition Z = -2f agar tombol berada di depan sprite / collider ruangan sehingga bisa di-klik
            thermGo.transform.localPosition = new Vector3(-2.2f, 1.1f, -2f);
            
            // Kompensasi skala terbalik dari Room parent (jika ada) saat membuat tombol
            Vector3 currentScale = Vector3.one;
            Vector3 parentScale = room.transform.lossyScale;
            if (parentScale.x < 0) currentScale.x = -currentScale.x;
            if (parentScale.y < 0) currentScale.y = -currentScale.y;
            thermGo.transform.localScale = currentScale;

            // Tambahkan Collider2D dengan ukuran yang wajar
            BoxCollider2D col = thermGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);
            col.isTrigger = true;

            // Tambahkan SpriteRenderer visual default termometer
            SpriteRenderer sr = thermGo.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sr.color = new Color(0.2f, 0.7f, 1f, 0.9f); // Warna biru muda
            sr.sortingOrder = 10;

            // Tambahkan component interaksi
            RoomThermometerButton btnComp = thermGo.AddComponent<RoomThermometerButton>();

            // Buat TextMeshPro display 3D melayang di bawah tombol
            GameObject textGo = new GameObject("TempText");
            textGo.transform.SetParent(thermGo.transform, false);
            textGo.transform.localPosition = new Vector3(0f, -0.8f, -0.1f);
            textGo.transform.localScale = Vector3.one; // Reset scale for text child

            TextMeshPro tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = "20°C";
            // Font size untuk objek 3D TextMeshPro tidak perlu terlalu besar. 2f-3f sudah pas.
            tmp.fontSize = 2.5f; 
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 11;

            // Set referensi text memakai SerializedObject agar tersimpan di Inspector
            SerializedObject btnSO = new SerializedObject(btnComp);
            btnSO.FindProperty("temperatureText").objectReferenceValue = tmp;
            btnSO.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(thermGo, "Create ThermometerButton inside room");
            roomSetupCount++;
        }

        // 5. Tandai Scene telah dimodifikasi agar bisa disave
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log($"[SetupThermometerTool] Sukses merebuild UI ThermometerPopup dan mengonfigurasi {roomSetupCount} ruangan.");
        EditorUtility.DisplayDialog("Setup Sukses", $"Berhasil merebuild UI ThermometerPopup dan memperbaiki tombol di {roomSetupCount} ruangan!\n\nSilakan tekan CTRL+S untuk menyimpan scene.", "OK");
    }
}
#endif
