# Infraestrutura como Código (Terraform)

O Terraform provisiona um cluster **Kubernetes local com Kind**, o **metrics-server** (necessário para o HPA) e o **PostgreSQL** dentro do cluster.

## Recursos criados

| Recurso | Provider | Descrição |
|---------|----------|-----------|
| `kind_cluster.wrenchbox` | Kind | Cluster Kubernetes local, com portas `8080` (API) e `8025` (MailHog) mapeadas no host |
| `null_resource.metrics_server` | local-exec | Instala o metrics-server com `--kubelet-insecure-tls` (exigido no Kind) |
| `kubernetes_namespace.wrenchbox` | Kubernetes | Namespace `wrenchbox` |
| `kubernetes_secret.wrenchbox` | Kubernetes | Senha do banco, JWT e webhook secret (valores de demonstração) |
| `kubernetes_persistent_volume_claim.postgres` | Kubernetes | Volume persistente do PostgreSQL |
| `kubernetes_deployment.postgres` | Kubernetes | Banco `postgres:16-alpine` |
| `kubernetes_service.postgres` | Kubernetes | Service ClusterIP na porta 5432 |

A API, o MailHog e o HPA ficam nos manifestos de [`../k8s`](../k8s) e são aplicados depois do `terraform apply`.

## Pré-requisitos

- Docker Desktop em execução
- [Kind](https://kind.sigs.k8s.io/docs/user/quick-start/)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [Terraform](https://developer.hashicorp.com/terraform/install) >= 1.6

## Como aplicar

```bash
cd infra
terraform init
terraform plan
terraform apply
```

O cluster fica disponível no kubeconfig padrão (`~/.kube/config`). Confirme com:

```bash
kubectl cluster-info --context kind-wrenchbox
kubectl get nodes
```

### Deploy da aplicação

```bash
docker build -t wrenchbox:latest .
kind load docker-image wrenchbox:latest --name wrenchbox
kubectl apply -k ../k8s
kubectl -n wrenchbox rollout status deployment/wrenchbox-api
```

- API: http://localhost:8080/swagger
- MailHog: http://localhost:8025
- Health: http://localhost:8080/health

### Destruir

```bash
terraform destroy
```

Isso remove o cluster Kind e todos os recursos provisionados.
