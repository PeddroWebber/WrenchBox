output "cluster_name" {
  description = "Kind cluster name."
  value       = kind_cluster.wrenchbox.name
}

output "kubeconfig_path" {
  description = "Kubeconfig written by the Kind provider."
  value       = kind_cluster.wrenchbox.kubeconfig_path
}

output "api_url" {
  description = "Local URL of the API after applying k8s manifests (NodePort 30080 -> localhost:8080)."
  value       = "http://localhost:8080"
}

output "swagger_url" {
  description = "Swagger UI."
  value       = "http://localhost:8080/swagger"
}

output "mailhog_url" {
  description = "MailHog UI after applying k8s manifests (NodePort 30025 -> localhost:8025)."
  value       = "http://localhost:8025"
}

output "postgres_service" {
  description = "In-cluster PostgreSQL service."
  value       = "${kubernetes_service.postgres.metadata[0].namespace}/${kubernetes_service.postgres.metadata[0].name}:5432"
}
