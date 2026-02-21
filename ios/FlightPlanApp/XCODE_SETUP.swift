// Swift Package Manager manifest — alternative to Xcode project
// Run: swift build  (from the ios/FlightPlanApp directory)
// Or open in Xcode: File → Open → select this directory

// NOTE: For a full Xcode project (.xcodeproj), create a new
// "App" project in Xcode targeting iOS 17+, then replace/add
// the Swift files from this directory into that project.
// The .xcodeproj format is binary and not practical to generate by hand.

// QUICK XCODE SETUP STEPS:
// 1. Open Xcode → File → New → Project → App
// 2. Product Name: FlightPlanApp
// 3. Interface: SwiftUI, Language: Swift, Minimum Deployments: iOS 17
// 4. Delete the default ContentView.swift
// 5. Drag all .swift files from this directory into the Xcode project navigator
// 6. Make sure "Copy items if needed" is checked
// 7. Set your simulator to iPhone 15
// 8. Cmd+R to build and run

// IMPORTANT: Add App Transport Security exception for localhost
// In Info.plist, add:
//   NSAppTransportSecurity → NSAllowsLocalNetworking = YES
// This allows the app to call http://localhost:5000 (non-HTTPS)
