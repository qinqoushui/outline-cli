outline wiki不提供markdown源码模式，使用API（https://www.getoutline.com/developers#tag/documents/POST/documents.list）导出到本地后通过typora等工具编辑后推回
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
4. 修改文件后再push回去，目前未测试拉取和推出单个文件

<img width="1113" height="721" alt="d8701a4d79c0f432e32bd7fb702cbc08" src="https://github.com/user-attachments/assets/5a0c8db3-e44f-4d52-ab82-442ade428ebf" />

