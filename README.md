# 💳 Payments API - Fase 3 (MVP AWS)

## 📌 Visão Geral

A **Payments API** é um microsserviço responsável pelo processamento e gerenciamento de pagamentos dentro do ecossistema da Fase 3.

Projetada com uma abordagem **serverless e cloud-native na AWS**, a API garante alta escalabilidade, resiliência e desacoplamento, permitindo integração eficiente com outros serviços da plataforma, como Users, Games e Notifications.

Este serviço representa a camada financeira do sistema, sendo crítico para operações transacionais.

---

## 🎯 Objetivo

* Processar e registrar transações de pagamento
* Garantir consistência e rastreabilidade das operações
* Integrar dados financeiros com outros microsserviços
* Suportar crescimento com escalabilidade automática

---

## 🏗️ Arquitetura

A aplicação segue o padrão de **Arquitetura Hexagonal (Ports & Adapters)**, garantindo isolamento entre regras de negócio e infraestrutura.

### 🔹 Camadas

* **Domain**

  * Entidades de pagamento
  * Regras de negócio (ex: validação de transações)
  * Interfaces (ports)

* **Application**

  * Casos de uso (ProcessPayment, GetPayment, etc.)
  * Orquestração de fluxos transacionais

* **Infrastructure**

  * Persistência (DynamoDB)
  * Integrações externas (gateways de pagamento, se aplicável)

* **API (EntryPoint)**

  * AWS Lambda handlers
  * Endpoints expostos via API Gateway

---

## ☁️ Infraestrutura AWS

O serviço utiliza componentes gerenciados da AWS para garantir alta disponibilidade:

* **AWS Lambda**

  * Execução dos fluxos de pagamento

* **Amazon API Gateway**

  * Exposição dos endpoints HTTP

* **Amazon DynamoDB**

  * Armazenamento das transações

* **AWS IAM**

  * Controle de permissões

* **AWS CloudWatch**

  * Logs, auditoria e monitoramento

---

## 🔗 Funcionalidades

* 💳 Registro de pagamentos
* 📄 Consulta de transações
* 🔍 Busca por usuário ou status
* ❌ Cancelamento de pagamento (quando aplicável)
* 📊 Rastreamento de status (pendente, aprovado, recusado)

---

## 🔐 Segurança e Consistência

Por se tratar de um domínio financeiro, algumas preocupações são fundamentais:

* Validação rigorosa de entrada
* Idempotência em operações críticas
* Controle de acesso via IAM
* Logging para auditoria
* Possível integração com serviços externos de pagamento

💡 Em cenários reais, integrações com provedores como Stripe, PayPal ou adquirentes são comuns.

---

## 🚀 Stack Tecnológica

* **.NET 8**
* **C#**
* **AWS Lambda**
* **API Gateway**
* **DynamoDB**
* **xUnit + Moq**

---

## ⚙️ Execução do Projeto

### 🔧 Pré-requisitos

* .NET 8 SDK
* AWS CLI configurado
* Conta AWS ativa
* Amazon Lambda Tools

---

### ▶️ Execução local

```bash id="9vxf8z"
dotnet restore
dotnet build
dotnet run
```

---

### ☁️ Deploy na AWS

```bash id="g7k8o9"
dotnet lambda deploy-serverless
```

## 📦 Estrutura do Projeto

```bash id="8wr03t"
src/
 ├── Domain/
 ├── Application/
 ├── Infrastructure/
 ├── API/
 └── Shared/
```

---

## 🔄 Integração com o Ecossistema

A Payments API se integra diretamente com:

* 👤 Users API → identificação do pagador
* 🎮 Games API → compra de jogos / itens
* 🔔 Notifications API → envio de status de pagamento
