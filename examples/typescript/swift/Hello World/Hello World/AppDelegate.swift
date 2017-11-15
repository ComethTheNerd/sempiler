import UIKit
@UIApplicationMain class AppDelegate: UIResponder, UIApplicationDelegate { public var window: UIWindow?
    public func application(_: UIApplication, didFinishLaunchingWithOptions _: [UIApplicationLaunchOptionsKey: Any]?) -> Bool { window = UIWindow(frame: UIScreen.main.bounds)
        var homeViewController: UIViewController = UIViewController()
        homeViewController.view.addSubview(IntroView())
        window!.rootViewController = homeViewController
        window!.makeKeyAndVisible()
        return true
    }
}
