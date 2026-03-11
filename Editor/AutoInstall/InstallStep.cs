namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 安装步骤枚举
    /// </summary>
    public enum InstallStep
    {
        None,
        CheckEnvironment,    // 检查环境
        InstallPackages,     // 安装包
        OpenScene,           // 打开场景
        Complete             // 完成
    }
}
