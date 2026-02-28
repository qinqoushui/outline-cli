outline wiki不提供markdown源码模式，使用API导出到本地后通过typora等工具编辑后推回
1. 在outline中配置权限
2. 编译项目，运行outline  config，自动创建config.json，修改config.json的相应内容
  ```
     {
    "api_url": "http://192.168.3.229:3000",
    "api_token": "ol_api_bTnu9H9eRQTAP2vpPh1zuacg1IjI4TunwJaSCz",
    "default_collection_id": "设计"
  }
  ```
3. 运行 outline pull ，拉取指定集合的文档到doc目录下
