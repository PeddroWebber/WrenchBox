variable "cluster_name" {
  description = "Name of the local Kind cluster."
  type        = string
  default     = "wrenchbox"
}

variable "postgres_storage" {
  description = "Requested storage for the in-cluster PostgreSQL PVC."
  type        = string
  default     = "1Gi"
}
