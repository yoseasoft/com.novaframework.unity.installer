# 统一游戏开发框架配置中心

Unity编辑器扩展工具，用于管理和配置统一游戏开发框架。

## 功能特性

1. **配置中心**：插件配置、环境目录配置、程序集配置管理
2. **包管理**：自动处理依赖关系，通过Git安装包
3. **自动安装**：一键安装必需包、创建目录、配置程序集、解压基础包
4. **环境验证**：验证包安装状态、目录结构、配置文件完整性
5. **导出配置**：导出完整配置到system_environments.json
6. **重置功能**：安全清理框架安装文件和配置

## 快捷菜单

- `Tools/配置中心` (Alt+C)
- `Tools/自动安装` (F8) 
- `Tools/验证环境`
- `Tools/导出配置` (F3)
- `Tools/重置安装` (Ctrl+Alt+R)

## 配置文件

- `Assets/Resources/system_environments.json`：导出的完整配置

## 注意事项

1. 确保系统已安装Git并添加到PATH环境变量
2. 需要先安装有odin
3. 需要先把HybridCLR放到packages目录下

## 文件结构

```
Assets/Editor/FrameworkInstaller/
├── AutoInstall/                          # 自动安装
│   └── AutoInstallManager.cs            # 自动安装管理器
├── Definitions/                         # 类型定义
│   ├── Constants.cs                     # 常量定义
│   ├── PackageXMLInfo.cs                # 包信息定义
│   └── SystemDataStructs.cs             # 系统数据结构
├── Data/                                # 数据管理
│   ├── DataManager.cs                   # 数据管理器
│   └── PackageManager.cs                # 包管理器
├── UI/                                  # 界面
│   ├── ConfigurationWindow.cs           # 主窗口
│   ├── DirectoryConfigurationView.cs    # 目录配置
│   ├── AssemblyConfigurationView.cs     # 程序集配置
│   └── PackageConfigurationView.cs      # 包配置
├── Tools/                               # 工具类
│   ├── EnvironmentValidator.cs          # 环境验证器
│   ├── GitHelper.cs                     # Git辅助类
│   ├── GitManager.cs                    # Git管理器
│   ├── PackageManifestHandler.cs        # 包清单处理器
│   ├── PackageXMLParser.cs              # 包XML解析器
│   ├── ResetManager.cs                  # 重置管理器
│   ├── RichTextUtils.cs                 # 富文本工具
│   └── ZipHelper.cs                     # ZIP工具
├── Menus/                               # 菜单入口
│   ├── MainMenu.cs                      # 主菜单
│   └── ExportConfigurationMenu.cs       # 导出配置菜单
└── Editor Default Resources/            # 默认资源
    ├── Aot/                             # AOT库文件
    ├── BasePack/                        # 基础包
    └── Config/                          # 配置文件
        ├── AppConfigures.asset          # 应用配置
        ├── AppSettings.asset            # 应用设置
        ├── repo_manifest.xml            # 包清单文件
        └── repo_manifest.xsd            # 包清单模式定义
```

