#compiler {
    addEntitlement("com.apple.developer.in-app-payments", ["merchant.sempiler.com"])
    addPermission("NSCameraUsageDescription", "This app wants to take pictures")
    addPermission("NSPhotoLibraryUsageDescription", "This app wants to use your photos")
}

let topSafeAreaHeight: CGFloat = 0.0;
let bottomSafeAreaHeight: CGFloat = 0.0;

export default function main()
{
    #codegen`if #available(iOS 11.0, *)
    {
        let window = UIApplication.shared.windows[0]
        let safeFrame = window.safeAreaLayoutGuide.layoutFrame
        topSafeAreaHeight = safeFrame.minY
        bottomSafeAreaHeight = window.frame.maxY - safeFrame.maxY
    }`

    return <AppView />
}

function AppView() : View
{
    return <Text text="Hello World" />
}