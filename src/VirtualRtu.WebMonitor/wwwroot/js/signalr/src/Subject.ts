// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IStreamResult, IStreamSubscriber, ISubscription } from "./Stream";
import { SubjectSubscription } from "./Utils";

/** Stream implementation to stream items to the server. */
export class Subject<T> implements IStreamResult<T> {
    /** @internal */
    observers: Array<IStreamSubscriber<T>>;

    /** @internal */
    cancelCallback?: () => Promise<void>;

    constructor() {
        this.observers = [];
    }

    next(item: T): void {
        for (const observer of this.observers) {
            observer.next(item);
        }
    }

    error(err: any): void {
        for (const observer of this.observers) {
            if (observer.error) {
                observer.error(err);
            }
        }
    }

    complete(): void {
        for (const observer of this.observers) {
            if (observer.complete) {
                observer.complete();
            }
        }
    }

    subscribe(observer: IStreamSubscriber<T>): ISubscription<T> {
        this.observers.push(observer);
        return new SubjectSubscription(this, observer);
    }
}