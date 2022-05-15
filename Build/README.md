# WebCrawler 部署文档

## 运行环境
- Linux Ubuntu 20.04 LTS
- .NET 6.0
- PostgreSQL

## 添加 Microsoft Package Signing Key
```Bash
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

## 安装 .NET 运行时
```Bash
sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-6.0
```

## 部署 WebCrawler

- 从 [GitHub](./) 下载最新 build，并将 Portable 目录中的文件拷贝至 Linux 环境的独立文件夹中。
  > 备注：该程序包支持跨平台运行。
  > - Windows 环境\
      运行 `WebCrawler.Console.exe`
  > - Linux 环境\
      运行 `dotnet WebCrawler.Console.dll`
- 创建定时任务执行命令 `dotnet WebCrawler.Console.dll`，可参考[文章](https://ubuntuhandbook.org/index.php/2021/05/create-schedule-tasks-ubuntu-daily-weekly-monthly-job/)。
  > 注意：考虑到网站数量较多时，文章抓取比较耗时，所以建议前期运行计划至少间隔24小时。

## 参考文档：
- https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu
