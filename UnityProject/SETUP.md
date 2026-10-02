# Abrindo o projeto na Unity (10 minutos)

> Importante: o código em `Assets/_Project/Scripts/` foi escrito e o **core foi testado
> headless** (25 testes em `tests/`), mas a camada Unity ainda não foi compilada dentro
> do editor — pequenos ajustes de compilação podem aparecer na primeira abertura.
> Cenas e prefabs são criados por você nos passos abaixo (arquivos binários/YAML da
> Unity não dão para gerar bem fora do editor).

## 1. Abrir

1. Instale **Unity 6.3 LTS** (módulo Android; iOS se tiver Mac) pelo Unity Hub.
2. Unity Hub → **Add project from disk** → selecione a pasta `UnityProject/`.
3. Se o Hub pedir confirmação de versão (o `ProjectVersion.txt` aponta 6000.3.0f1),
   escolha a sua 6000.3.x instalada e confirme o upgrade.
4. Primeira abertura baixa os pacotes do `Packages/manifest.json` (URP, Input System,
   2D). Quando o Input System pedir para reiniciar o editor em modo "novo input",
   aceite.

## 2. Configurar (uma vez)

1. **Layer**: crie a layer `Solid` (Project Settings → Tags and Layers).
2. **Tick fixo**: Project Settings → Time → Fixed Timestep = `0.0166667` (60Hz).
3. **URP**: Assets → Create → Rendering → URP Asset (with 2D Renderer); aponte em
   Project Settings → Graphics. (Se criou o projeto pelo template 2D URP, já existe.)
4. **Tuning**: Project → Create → `Two/Player Tuning` → salve como
   `Assets/_Project/Settings/PlayerTuning.asset`. Os números já vêm calibrados.

## 3. Cena playground (Fase 0)

1. Nova cena `Assets/_Project/Scenes/Playground.unity`.
2. **Chão/paredes**: GameObjects com `BoxCollider2D`, layer `Solid` (ou Tilemap).
3. **Player**: GameObject `Player` com:
   - `SpriteRenderer` (qualquer cápsula/quadrado por enquanto)
   - `PlayerAvatar` → arraste o `PlayerTuning.asset`, marque `Solid` no solidMask
4. **Ganchos**: GameObjects vazios com `HookPoint` espalhados no alto.
5. **Corda de balanço**: filho do Player com `LineRenderer` (width ~0.08, material
   Sprites/Default, cor clara) + `RopeView` (mode = SwingAnchor, player = Player).
6. **Câmera**: na Main Camera (Projection = Orthographic), adicione `CameraRig`
   → player = Player.
7. Play: **A/D** move, **Espaço** pula, **J** (segurar e soltar) arremessa o fio,
   **K** fixa. No device (Build & Run), os controles touch assumem sozinhos.

## 4. Validar o feel

A referência de sensação é o protótipo HTML em `prototype/` (mesmos números).
Ajuste pelo Inspector no asset de tuning — as mudanças vão direto para o core.

## O que vem depois (Fase 1 — docs/04)

- Importar o SDK do Photon Fusion 2 e implementar `FusionPartnerProvider`
  seguindo `Assets/_Project/Scripts/Unity/Net/FUSION-INTEGRATION.md`.
- Portar os testes do core para o Unity Test Framework se quisermos rodá-los
  também dentro do editor (os fontes já são os mesmos).
