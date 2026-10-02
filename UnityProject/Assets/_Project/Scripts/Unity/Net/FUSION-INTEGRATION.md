# Integração Photon Fusion 2 (Fase 1)

A camada de rede implementa `IPartnerProvider` — o gameplay não muda uma linha.
Este arquivo mapeia o que será feito quando o SDK do Fusion 2 for importado
(Photon Dashboard → criar app Fusion → App Id em `PhotonAppSettings`).

## Mapeamento

| Conceito do projeto | Fusion 2 (Shared Mode) |
|---|---|
| Sala por código de 6 letras | `StartGameArgs { GameMode = GameMode.Shared, SessionName = codigo }` |
| Autoridade do próprio personagem | Objeto spawnado pelo próprio cliente → `HasStateAuthority == true` |
| Snapshot replicado (`SelfSnapshot`) | `[Networked]` properties num `NetworkBehaviour`: posição, velocidade, `State`, `FacingX`, flag `IsPinned` |
| Interpolação do parceiro | Render timeframe REMOTO do Fusion (interpolação embutida nos snapshots) |
| `PartnerInfo` consumido pela sim | Ler as `[Networked]` do objeto do parceiro no tick local |
| Reconexão (60s, estado Dormant) | `runner.Shutdown` detectado → reconectar com mesmo `SessionName`; enquanto isso o `IPartnerProvider` reporta `Present=false` e a sim local vira `Dormant` no boneco remoto |
| Região São Paulo | `FixedRegion = "sa"` nos `AppSettings` do Photon |
| Simulação de rede ruim (obrigatória!) | Network Conditions do Fusion: 150ms RTT + 30ms jitter + 2% loss |

## Regras ao implementar `FusionPartnerProvider`

1. O `PlayerAvatar` do próprio jogador continua rodando `PlayerSim` localmente —
   o Fusion só CARREGA o snapshot para fora (`PublishSelf` escreve nas `[Networked]`).
2. O avatar do parceiro NÃO roda `PlayerSim`: é um boneco puramente visual posicionado
   pela interpolação do Fusion + `RopeView`. (Toda a física dele roda no celular dele.)
3. Objetos de puzzle: `NetworkObject` com transferência de autoridade
   (`Object.RequestStateAuthority()`) para quem tocar primeiro; plano B documentado
   em docs/03 (autoridade fixa no criador da sala).
4. NUNCA usar `Time.time` para lógica compartilhada — tick do `NetworkRunner`.
5. O asmdef `Two.Unity.Fusion` (criar junto com o SDK) referencia `Two.Unity` e
   `Fusion.Runtime`; nada no sentido contrário, para o projeto compilar sem o SDK.
