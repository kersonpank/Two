# 04 — Roadmap do MVP

Princípio: **atacar o maior risco primeiro.** Os dois riscos que podem matar o projeto são (1) o movimento não ficar gostoso e (2) a corda online não funcionar em rede móvel real. Conteúdo, arte e meta vêm depois — são trabalho, não risco.

Cada fase tem um **gate**: critério objetivo de saída. Não se avança com gate vermelho; se um gate provar-se inatingível, o plano muda (ver "saídas de emergência" no doc 03).

## Fase 0 — Feel (2–3 semanas) · offline, 1 cena cinza

Movimento + corda num celular só. Caixas cinzas, zero arte, zero rede.

- Controller completo: correr, pular (coyote/buffer/altura variável), escalar borda.
- Arremesso de fio com câmera lenta de mira + **balanço em gancho** (pêndulo analítico) + rapel.
- Joystick flutuante + botões; haptics básicos.
- Tuning por ScriptableObject + cena de playground.

**Gate 0:** três pessoas que não são o dev jogam o playground no celular e **pedem para continuar jogando**. Balançar e soltar no ápice tem que arrancar um sorriso. Se precisar de mais 2 semanas aqui, são as 2 semanas mais bem gastas do projeto.

## Fase 1 — Corda online (4–5 semanas) · o coração técnico

- Fusion 2 Shared Mode: sala por código, spawn da dupla.
- Replicação do personagem + interpolação do parceiro.
- `RopeConstraint` (tether suave) + `RopeVisual` (verlet) entre os dois jogadores.
- Pin + balanço a partir do parceiro fixado; puxar objeto com transferência de autoridade.
- Ciclo de vida mobile: background → `Dormant` → reconexão em 60s; pausa por consentimento.
- Overlay de rede + aprovação de tudo em **150ms/30ms jitter/2% loss**.

**Gate 1:** dois celulares, um no Wi-Fi e outro em 4G, atravessam um percurso-teste de 5 min usando todas as mecânicas; app em background por 30s no meio e a sessão sobrevive; nenhum dos dois jogadores percebe "borracha" no próprio personagem (zero, não "pouco").

## Fase 2 — Capítulo vertical (6–8 semanas) · o jogo de verdade

- **Capítulo 1 completo: 5 níveis** (5–8 min cada) com curva de introdução de mecânicas: mover → tether → balanço → pin → contrapeso → final que usa tudo em dupla.
- Direção de arte aplicada: espíritos, fio de luz, luz-por-proximidade, 1 bioma.
- Morte/checkpoint (puxado pelo fio), colecionáveis co-op (1 tipo).
- Emotes, ping contextual, botão de sincronia 3-2-1.
- Câmera final (peso do parceiro, zoom dinâmico), áudio e música do bioma.
- Onboarding **jogável em dupla** (nível 1 É o tutorial; nenhuma tela de texto).

**Gate 2:** 5 duplas reais (recrutadas, não amigas do dev) terminam o capítulo remotamente, cada uma em sua casa, sem ajuda. Mede-se: % de conclusão, onde travaram, e a pergunta única "jogaria o capítulo 2 com essa pessoa?" — alvo: 4 de 5 dizem sim.

## Fase 3 — Meta + soft launch BR (4–6 semanas)

- Contas (anônimo → vincular Google/Apple), Cloud Save, progresso da dupla.
- Deep link de convite via WhatsApp (fluxo de 2 minutos do doc 01).
- Streak da dupla + foto de fim de capítulo compartilhável.
- Cosméticos v1 (cores de fio ganhas jogando; IAP ainda desligado).
- Analytics (funil: install → sala criada → parceiro entrou → nível 1 completo → capítulo completo → D1/D7 da dupla), Crashlytics, Remote Config.
- **Soft launch: Google Play, Brasil apenas.** iOS logo depois (TestFlight durante).

**Gate 3 (números de soft launch para seguir investindo):**
- Crash-free sessions > 99%; 60fps no aparelho-alvo em > 90% do tempo.
- Conversão "instalou → jogou com alguém" > 35% (é A métrica deste jogo: medir e otimizar o funil do convite antes de qualquer outra coisa).
- D7 de duplas formadas > 15%.

## Fase 4 — Crescer (contínuo)

Capítulos 2+, desafio diário, Friend Pass + IAP de cosméticos, iOS release, novos biomas/mecânicas das "gavetas" do doc 01 — priorizado pelos dados da Fase 3, não por achismo.

## Explicitamente FORA do MVP (anotar e não ceder)

- Matchmaking com estranhos · chat de voz/texto · 3+ jogadores · PvP ou ranking global
- Single-player · editor de níveis · localização além de PT-BR + EN
- Android < 8.0 · tablets como alvo (funciona, mas não se otimiza)

## Riscos de projeto (além dos técnicos do doc 03)

| Risco | Mitigação |
|---|---|
| Escopo de conteúdo explode (level design é o trabalho longo) | Capítulo 1 fecha o MVP; kit de peças reusáveis de level design desde a Fase 2 |
| "Precisa de 2 para jogar" trava a primeira sessão | Todo o polimento do funil de convite (Fase 3) existe para isso; Friend Pass depois |
| IP: semelhança excessiva com Unravel | Tema fio-de-luz, personagens não-têxteis, nome próprio; revisar trade dress antes da loja |
| Dev solo em projeto de 5–6 meses | Gates impedem meses em rumo errado; Fase 0/1 são pequenas de propósito para falhar barato |

## Semana 1 (ações concretas)

1. Projeto Unity 6.3 LTS + URP (template 2D), git LFS configurado, `.gitignore` de Unity.
2. Pacotes: Input System, Cinemachine 3, dependências Fusion 2 (SDK via Photon Dashboard — criar conta e app id já).
3. Cena playground cinza + controller v0 (andar/pular com assists).
4. Verlet rope visual v0 pendurada num gancho (validar o look do fio de luz com Light2D cedo — é a identidade visual do jogo).
