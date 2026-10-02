using UnityEngine;
using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// Câmera da dupla (docs/01): segue o PRÓPRIO jogador, com peso gravitacional em
    /// direção ao parceiro e zoom dinâmico pela distância — dentro do alcance do fio,
    /// os dois quase sempre na mesma tela. Sem dependência de Cinemachine na Fase 0;
    /// migrar depois se precisarmos de coisas chiques (confiner, shake em noise).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] PlayerAvatar player;

        [Header("Enquadramento")]
        [SerializeField] float partnerWeight = 0.35f;   // 0 = ignora parceiro, 0.5 = ponto médio
        [SerializeField] float lookAheadX = 1.2f;       // adianta na direção do movimento
        [SerializeField] float followSmoothTime = 0.18f;

        [Header("Zoom")]
        [SerializeField] float baseOrthoSize = 5.0f;
        [SerializeField] float zoomPerMeter = 0.22f;    // cresce com a distância da dupla
        [SerializeField] float maxOrthoSize = 8.5f;
        [SerializeField] float zoomSmoothTime = 0.35f;

        Camera _cam;
        Vector3 _followVelocity;
        float _zoomVelocity;

        void Awake() => _cam = GetComponent<Camera>();

        void LateUpdate()
        {
            Vector2 self = player.RenderPosition;
            Vector2 target = self;
            float targetSize = baseOrthoSize;

            if (player.Partner.CurrentPartner.Present)
            {
                Vector2 partner = player.Partner.PartnerRenderPosition.ToUnity();
                target = Vector2.Lerp(self, partner, partnerWeight);
                targetSize = Mathf.Min(
                    baseOrthoSize + Vector2.Distance(self, partner) * zoomPerMeter,
                    maxOrthoSize);
            }

            target.x += player.Sim.FacingX * lookAheadX;

            Vector3 desired = new Vector3(target.x, target.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref _followVelocity, followSmoothTime);

            _cam.orthographicSize = Mathf.SmoothDamp(
                _cam.orthographicSize, targetSize, ref _zoomVelocity, zoomSmoothTime);
        }
    }
}
