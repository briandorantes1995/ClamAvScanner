# Tell terraform to use the provider and select a version.
terraform {
  required_providers {
      
    hcloud = {
      source  = "hetznercloud/hcloud"
      version = "~> 1.45"
    }

  hcp = {
          source = "hashicorp/hcp"
        }

  local = {
      source = "hashicorp/local"
    }

  }
}

# Set the variable value in *.tfvars file
# or using the -var="hcloud_token=..." CLI option
variable "hcloud_token" {
  sensitive = true
}

locals {
  location = "nbg1"
}

# Configure the Hetzner Cloud Provider
provider "hcloud" {
  token = var.hcloud_token
}

data "hcloud_firewall" "fw"{
  name= "general"
}

data "hcloud_ssh_key" "mykey"{
  name = "Hetzner"
}

data "hcp_packer_artifact" "base" {
  bucket_name   = "BaseImage"
  channel_name  = "latest"
  platform      = "hetznercloud"
  region        = ""
}

resource "hcloud_server" "node1" {
  name         = "ClamAvScanner"
  image        = data.hcp_packer_artifact.base.external_identifier
  location = local.location
  server_type  = "cx23"
  public_net {
    ipv4_enabled = true
    ipv6_enabled = false
  }
  firewall_ids = [data.hcloud_firewall.fw.id]
  ssh_keys = [data.hcloud_ssh_key.mykey.id]
  user_data = <<-EOF
  #cloud-config
  
  write_files:
    - path: /usr/local/bin/bootstrap.sh
      permissions: '0755'
      content: |
        mkdir -p /home/brian/.ssh
        cp /root/.ssh/authorized_keys /home/brian/.ssh/authorized_keys
        chown -R brian:brian /home/brian/.ssh
        chmod 700 /home/brian/.ssh
        chmod 600 /home/brian/.ssh/authorized_keys
  
        sed -i 's/^#*PermitRootLogin.*/PermitRootLogin no/' /etc/ssh/sshd_config
        systemctl restart ssh
  
  runcmd:
    - /usr/local/bin/bootstrap.sh
  EOF
}

output "server_ip" {
  value = hcloud_server.node1.ipv4_address
}

# Create Ansible inventory
resource "local_file" "deploy_inventory" {
  content = <<EOF
[servers]
ClamAvScanner ansible_host=${hcloud_server.node1.ipv4_address} ansible_user=brian
EOF

  filename = "../Ansible/inventory.ini"
}
