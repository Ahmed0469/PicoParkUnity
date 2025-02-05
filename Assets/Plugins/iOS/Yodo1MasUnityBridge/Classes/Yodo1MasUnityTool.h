//
//  Yodo1Alert.h
//
//
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

#ifdef __cplusplus
extern "C" {
#endif
    
    extern void UnitySendMessage(const char* obj, const char* method, const char* msg);
#if UNITY_VERSION < 500
    extern void UnityPause(bool pause);
#else
    extern void UnityPause(int pause);
#endif
    extern NSString* Yodo1MasConvertCharToNSString(const char* string);
    extern char* Yodo1MasConvertNSStringToChar(NSString* string);
    
#ifdef __cplusplus
}
#endif

