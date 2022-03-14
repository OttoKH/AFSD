# Apartment-Friendly-Smart-Door


sudo apt update
sudo apt install -y unclutter libx11-dev libvlc-dev
sudo rm /etc/xdg/lxsession/LXDE-pi/sshpwd.sh

cd ~
wget https://download.visualstudio.microsoft.com/download/pr/1b2a2fb1-b04b-485b-8a25-caed97ebe601/0c6024a3814f664558dc39fc85b34192/dotnet-sdk-3.1.416-linux-arm.tar.gz
sudo mkdir -p dotnet
sudo tar zxf dotnet-sdk-3.1.416-linux-arm.tar.gz -C dotnet
sudo echo export DOTNET_ROOT=$HOME/dotnet >> .bashrc
sudo echo export PATH=$PATH:$HOME/dotnet >> .bashrc
