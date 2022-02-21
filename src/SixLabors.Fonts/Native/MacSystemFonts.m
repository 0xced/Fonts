// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

#import <CoreText/CoreText.h>
#import <Foundation/Foundation.h>

typedef NS_ENUM(int32_t, MoveNextStatusCode)
{
    MoveNextStatusSuccess = 0,
    MoveNextStatusBufferTooSmall = 1,
};

void __cdecl StartIterating(int32_t iteratorId);
MoveNextStatusCode __cdecl MoveNext(int32_t iteratorId, uint8_t *fontPath, NSUInteger *fontPathLength);
void __cdecl Reset(int32_t iteratorId);
void __cdecl EndIterating(int32_t iteratorId);

__attribute__((visibility("hidden")))
@interface FontsIterator : NSObject

@property (nonatomic, readonly) CFArrayRef fontURLs;

@property (nonatomic, assign) CFIndex cursor;

@end

@implementation FontsIterator

@synthesize fontURLs = _fontURLs;
@synthesize cursor = _cursor;

- (instancetype) init
{
    if (!(self = [super init]))
        return nil;

    _fontURLs = CTFontManagerCopyAvailableFontURLs();

    return self;
}

- (void) dealloc
{
    CFRelease(_fontURLs);
}

- (NSString *) description
{
    return [NSString stringWithFormat:@"%@ %@/%@", super.description, @(self.cursor), @(CFArrayGetCount(self.fontURLs))];
}

@end

static NSMutableDictionary<NSNumber*, FontsIterator*> *iterators;
static NSInteger debugLevel;

static FontsIterator * GetValidFontsIterator(int32_t iteratorId, NSString *sourceMethod)
{
    FontsIterator *iterator;
    @synchronized (iterators)
    {
        iterator = iterators[@(iteratorId)];
    }
    if (iterator == nil)
    {
        NSString *reason = [NSString stringWithFormat:@"The %2$@ method was called with an invalid iterator id (%1$@). Either StartIterating(%1$@) was not called before calling %2$@(%1$@) or %2$@(%1$@) was called after EndIterating(%1$@)", @(iteratorId), sourceMethod];
        @throw [NSException exceptionWithName:@"SixLabors.Fonts.Native" reason:reason userInfo:nil];
    }
    return iterator;
}

static void __attribute__((constructor)) initialize(void)
{
    iterators = [NSMutableDictionary new];
    debugLevel = NSProcessInfo.processInfo.environment[@"SIXLABORS_FONTS_NATIVE_DEBUG_LEVEL"].integerValue;
}

void __cdecl StartIterating(int32_t iteratorId)
{
    @synchronized (iterators)
    {
        if (debugLevel > 0)
        {
            NSLog(@"StartIterating(%@) [Entry]\n%@", @(iteratorId), iterators);
        }

        FontsIterator *iterator = iterators[@(iteratorId)];
        if (iterator != nil)
        {
            NSString *reason = [NSString stringWithFormat:@"The StartIterating method was called with an invalid iterator id (%1$@). EndIterating(%1$@) must be called first before calling StartIterating(%1$@) again.", @(iteratorId)];
            @throw [NSException exceptionWithName:@"SixLabors.Fonts.Native" reason:reason userInfo:nil];
        }
        iterators[@(iteratorId)] = [FontsIterator new];

        if (debugLevel > 0)
        {
            NSLog(@"StartIterating(%@) [Exit]\n%@", @(iteratorId), iterators);
        }
    }
}

MoveNextStatusCode __cdecl MoveNext(int32_t iteratorId, uint8_t *fontPath, NSUInteger *fontPathLength)
{
    FontsIterator *iterator = GetValidFontsIterator(iteratorId, @"MoveNext");

    if (debugLevel > 1)
    {
        NSLog(@"MoveNext(%@) [Entry]\n%@", @(iteratorId), iterator);
    }

    if (iterator.cursor < CFArrayGetCount(iterator.fontURLs))
    {
        NSUInteger maxLength = *fontPathLength;
        NSURL *url = CFArrayGetValueAtIndex(iterator.fontURLs, iterator.cursor);
        NSString *path = url.path;
        *fontPathLength = [path lengthOfBytesUsingEncoding:NSUTF8StringEncoding];
        NSRange remainingRange;
        [path getBytes:fontPath maxLength:maxLength usedLength:NULL encoding:NSUTF8StringEncoding options:0 range:NSMakeRange(0, path.length) remainingRange:&remainingRange];
        if (remainingRange.length > 0)
        {
            if (debugLevel > 1)
            {
                NSLog(@"MoveNext(%@) [Exit BufferTooSmall]\n%@\n%@", @(iteratorId), path, iterator);
            }
            return MoveNextStatusBufferTooSmall;
        }
        iterator.cursor++;
    }
    else
    {
        *fontPathLength = 0;
    }

    if (debugLevel > 1)
    {
        NSLog(@"MoveNext(%@) [Exit Success]\n%@\n%@", @(iteratorId), [[NSString alloc] initWithBytesNoCopy:fontPath length:*fontPathLength encoding:NSUTF8StringEncoding freeWhenDone:NO], iterator);
    }

    return MoveNextStatusSuccess;
}

void __cdecl Reset(int32_t iteratorId)
{
    FontsIterator *iterator = GetValidFontsIterator(iteratorId, @"Reset");

    if (debugLevel > 0)
    {
        NSLog(@"Reset(%@) [Entry]\n%@", @(iteratorId), iterator);
    }

    iterator.cursor = 0;

    if (debugLevel > 0)
    {
        NSLog(@"Reset(%@) [Exit]\n%@", @(iteratorId), iterator);
    }
}

void __cdecl EndIterating(int32_t iteratorId)
{
    @synchronized (iterators)
    {
        FontsIterator *iterator = iterators[@(iteratorId)];

        if (debugLevel > 0)
        {
            NSLog(@"EndIterating(%@) [Entry]\n%@", @(iteratorId), iterators);
        }

        if (iterator == nil)
        {
            NSString *reason = [NSString stringWithFormat:@"The EndIterating method was called with an invalid iterator id (%1$@). Either StartIterating(%1$@) was not called or EndIterating(%1$@) was already called.", @(iteratorId)];
            @throw [NSException exceptionWithName:@"SixLabors.Fonts.Native" reason:reason userInfo:nil];
        }
        [iterators removeObjectForKey:@(iteratorId)];

        if (debugLevel > 0)
        {
            NSLog(@"EndIterating(%@) [Exit]\n%@", @(iteratorId), iterators);
        }
    }
}

#if 0
int main(int argc, char *argv[])
{
    @autoreleasepool
    {
        setenv("SIXLABORS_FONTS_NATIVE_DEBUG_LEVEL", "1", 0);

        StartIterating(1);

        uint8_t *fontPath;
        int i = 1;
        int pathSize = PATH_MAX;
        while (true)
        {
            fontPath = calloc(1, pathSize);
            NSUInteger fontPathLength = pathSize;
            MoveNextStatusCode status = MoveNext(1, fontPath, &fontPathLength);
            if (status == MoveNextStatusBufferTooSmall)
            {
                printf("Retrying with size = %tu\n", fontPathLength);
                fontPath = realloc(fontPath, fontPathLength);
                status = MoveNext(1, fontPath, &fontPathLength);
            }

            if (status != MoveNextStatusSuccess)
            {
                printf("ABORTING WITH ERROR %d\n", status);
                free(fontPath);
                break;
            }

            if (fontPathLength == 0)
            {
                free(fontPath);
                break;
            }

            printf("%4d) %s\n", i++, [[NSString alloc] initWithBytesNoCopy:fontPath length:fontPathLength encoding:NSUTF8StringEncoding freeWhenDone:YES].UTF8String);
        }

        EndIterating(1);
    }
}
#endif
