terraform {
  required_version = ">= 1.6.0"

  required_providers {
    kind = {
      source  = "tehcyx/kind"
      version = "0.6.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.32"
    }
    null = {
      source  = "hashicorp/null"
      version = "~> 3.2"
    }
  }
}

provider "kind" {}

provider "kubernetes" {
  host                   = kind_cluster.wrenchbox.endpoint
  client_certificate     = kind_cluster.wrenchbox.client_certificate
  client_key             = kind_cluster.wrenchbox.client_key
  cluster_ca_certificate = kind_cluster.wrenchbox.cluster_ca_certificate
}
