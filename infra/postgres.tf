resource "kubernetes_namespace" "wrenchbox" {
  metadata {
    name = "wrenchbox"
  }

  depends_on = [kind_cluster.wrenchbox]
}

resource "kubernetes_secret" "wrenchbox" {
  metadata {
    name      = "wrenchbox-secret"
    namespace = kubernetes_namespace.wrenchbox.metadata[0].name
  }

  data = {
    POSTGRES_DB               = "wrenchbox"
    POSTGRES_USER             = "postgres"
    POSTGRES_PASSWORD         = "postgres"
    Jwt__Secret               = "WrenchBox_K8s_Secret_Key_Min_32_Chars!!"
    Webhook__Secret           = "dev-webhook-secret"
    ConnectionStrings__Default = "Host=postgres;Port=5432;Database=wrenchbox;Username=postgres;Password=postgres"
  }

  type = "Opaque"
}

resource "kubernetes_persistent_volume_claim" "postgres" {
  metadata {
    name      = "postgres-pvc"
    namespace = kubernetes_namespace.wrenchbox.metadata[0].name
  }

  spec {
    access_modes = ["ReadWriteOnce"]
    resources {
      requests = {
        storage = var.postgres_storage
      }
    }
  }

  wait_until_bound = false
}

resource "kubernetes_deployment" "postgres" {
  metadata {
    name      = "postgres"
    namespace = kubernetes_namespace.wrenchbox.metadata[0].name
    labels = {
      app = "postgres"
    }
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        app = "postgres"
      }
    }

    template {
      metadata {
        labels = {
          app = "postgres"
        }
      }

      spec {
        container {
          name  = "postgres"
          image = "postgres:16-alpine"

          port {
            container_port = 5432
          }

          env {
            name = "POSTGRES_DB"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.wrenchbox.metadata[0].name
                key  = "POSTGRES_DB"
              }
            }
          }

          env {
            name = "POSTGRES_USER"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.wrenchbox.metadata[0].name
                key  = "POSTGRES_USER"
              }
            }
          }

          env {
            name = "POSTGRES_PASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.wrenchbox.metadata[0].name
                key  = "POSTGRES_PASSWORD"
              }
            }
          }

          volume_mount {
            name       = "data"
            mount_path = "/var/lib/postgresql/data"
          }

          resources {
            requests = {
              cpu    = "50m"
              memory = "128Mi"
            }
            limits = {
              cpu    = "500m"
              memory = "512Mi"
            }
          }
        }

        volume {
          name = "data"
          persistent_volume_claim {
            claim_name = kubernetes_persistent_volume_claim.postgres.metadata[0].name
          }
        }
      }
    }
  }
}

resource "kubernetes_service" "postgres" {
  metadata {
    name      = "postgres"
    namespace = kubernetes_namespace.wrenchbox.metadata[0].name
  }

  spec {
    selector = {
      app = "postgres"
    }

    port {
      port        = 5432
      target_port = 5432
    }
  }
}

resource "null_resource" "metrics_server" {
  depends_on = [kind_cluster.wrenchbox]

  provisioner "local-exec" {
    command = "kubectl apply -f ${path.module}/metrics-server.yaml"
  }
}
