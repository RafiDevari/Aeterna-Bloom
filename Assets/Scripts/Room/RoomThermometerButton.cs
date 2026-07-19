using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class RoomThermometerButton : MonoBehaviour
{
      [Header("Optional Display")]
      [SerializeField] private TextMeshPro temperatureText;

      private Room parentRoom;

      private void Start()
      {
          parentRoom = GetComponentInParent<Room>();
          if (parentRoom == null)
          {
              Debug.LogError($"[RoomThermometerButton] Tidak menemukan Room di parent dari {name}!");
              return;
          }

          parentRoom.OnTemperatureChanged += HandleTemperatureChanged;
          UpdateDisplay(parentRoom.Temperature);

          // Kompensasi jika parent di-flip (skala X/Y negatif) agar text dan visual tidak terbalik
          Vector3 currentScale = transform.localScale;
          if (transform.parent != null)
          {
              Vector3 parentScale = transform.parent.lossyScale;
              if (parentScale.x < 0) currentScale.x = -Mathf.Abs(currentScale.x);
              if (parentScale.y < 0) currentScale.y = -Mathf.Abs(currentScale.y);
              transform.localScale = currentScale;
          }
      }

      private void OnDestroy()
      {
          if (parentRoom != null)
          {
              parentRoom.OnTemperatureChanged -= HandleTemperatureChanged;
          }
      }

      private void HandleTemperatureChanged(float temp)
      {
          UpdateDisplay(temp);
      }

      private void UpdateDisplay(float temp)
      {
          if (temperatureText != null)
          {
              temperatureText.text = $"{temp:F0}°C";
          }
      }

      private void OnMouseUp()
      {
          if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
          {
              return;
          }

          if (parentRoom != null && ThermometerPopup.Instance != null)
          {
              ThermometerPopup.Instance.Open(parentRoom);
          }
          else
          {
              Debug.LogWarning($"[RoomThermometerButton] Klik terdeteksi tapi ThermometerPopup.Instance null atau parentRoom null.");
          }
      }
}
