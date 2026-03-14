# Workflows (Payments API)

Estes workflows são do **projeto Payments API** e devem rodar quando este projeto for a **raiz do repositório**.

- **`ci.yml`** — Restore, build (Release) e testes em `push` e `pull_request` na `junonn/mvp-aws`.
- **`publish-image.yml`** — Em `push` na `junonn/mvp-aws`: build da imagem Docker, push no ECR, disparo do `Fase3-InfraOrchestrador`.

Se o repositório for um **monorepo**, o GitHub só executa workflows em `.github/workflows` na **raiz**. Copie estes arquivos para a raiz e ajuste os caminhos (ex.: `Fase3-PaymentsAPI/Fcg.Payments.slnx`).

**Variables e secrets:** ver documentação do `Fase3-InfraOrchestrador` ou `docs/CI-CD.md`.  
**Valores padrão ECR:** se não definir variables, usam-se `AWS_REGION=us-east-1` e `ECR_REPOSITORY_NAME=fcg/Fase03` (detalhes em `Fase3-UsersAPI/.github/workflows/README.md`).
