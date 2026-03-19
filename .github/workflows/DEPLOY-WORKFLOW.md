# Workflow de Deploy — FCG Fenix Payments API

Este documento descreve o workflow **deploy.yml** (testes → build Docker → push ECR → deploy EC2 via reusable do repositório de infra) e o que alterar para **usersapi** e **gamesapi** se for replicar a partir deste repositório.

---

## 1. Workflow para Payments API (este repositório)

O arquivo **`.github/workflows/deploy.yml`** está configurado para **paymentsapi**:

- **Branch de deploy:** `junonn/mvp-aws`
- **OIDC:** secret `AWS_ROLE_ARN` (role com permissão ECR + SSM)
- **Testes:** `dotnet restore` → `dotnet build` → `dotnet test` em `Fcg.Payments.slnx`
- **Imagem:** build com `Dockerfile.postgres`, tag = `github.sha`
- **ECR:** repositório `fcg-fenix-paymentsapi-ecr`
- **Deploy:** chama o reusable do repo de infra com `aws_region`, `environment`, `service`, `repository`, `image_tag`

**Variáveis de repositório necessárias:**

| Variável      | Exemplo                         | Uso |
|---------------|----------------------------------|-----|
| `INFRA_REPO`  | `sua-org/Fase3-InfraOrchestrador` | Repositório que contém o reusable `deploy-ec2.yml` (owner/repo). |
| `AWS_REGION`  | `us-east-1`                     | Região AWS (opcional; default us-east-1). |

**Secrets:**

| Secret          | Uso |
|-----------------|-----|
| `AWS_ROLE_ARN`  | ARN da role OIDC (ECR push + SSM SendCommand + ec2:DescribeInstances). |

---

## 2. Paridade com Users API e Games API

Os repositórios **UsersAPI** e **GamesAPI** seguem o mesmo padrão; apenas mudam:

- **Users:** `SERVICE=usersapi`, `ECR_REPOSITORY_NAME=fcg-fenix-usersapi-ecr`, solution `Fcg.Users.slnx`
- **Games:** `SERVICE=gamesapi`, `ECR_REPOSITORY_NAME=fcg-fenix-gamesapi-ecr`, solution `Fcg.Games.slnx`

As variáveis `INFRA_REPO` e `AWS_REGION` e o secret `AWS_ROLE_ARN` são os mesmos nos três repositórios.

---

## 3. Boas práticas

- **SERVICE:** sempre `usersapi`, `gamesapi` ou `paymentsapi` (minúsculo, sem hífen).
- **ECR_REPOSITORY_NAME:** sempre `fcg-fenix-{service}-ecr`.
- **Tag da imagem:** sempre `github.sha`.
- **Dockerfile:** usar `Dockerfile.postgres` para a imagem all-in-one (Postgres + API) publicada no ECR.

Ver também o **README-OPERACIONAL.md** do repositório de infraestrutura (seções 5, 13 e 14).
