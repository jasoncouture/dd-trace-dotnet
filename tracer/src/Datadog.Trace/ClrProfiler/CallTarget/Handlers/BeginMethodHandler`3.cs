// <copyright file="BeginMethodHandler`3.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
#pragma warning disable SA1649 // File name must match first type name

namespace Datadog.Trace.ClrProfiler.CallTarget.Handlers
{
    internal static class BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3>
    {
        private static readonly InvokeDelegate _invokeDelegate;

        static BeginMethodHandler()
        {
            try
            {
                Type tArg1ByRef = typeof(TArg1).IsByRef ? typeof(TArg1) : typeof(TArg1).MakeByRefType();
                Type tArg2ByRef = typeof(TArg2).IsByRef ? typeof(TArg2) : typeof(TArg2).MakeByRefType();
                Type tArg3ByRef = typeof(TArg3).IsByRef ? typeof(TArg3) : typeof(TArg3).MakeByRefType();
                DynamicMethod dynMethod = IntegrationMapper.CreateBeginMethodDelegate(typeof(TIntegration), typeof(TTarget), new[] { tArg1ByRef, tArg2ByRef, tArg3ByRef });
                if (dynMethod != null)
                {
                    _invokeDelegate = (InvokeDelegate)dynMethod.CreateDelegate(typeof(InvokeDelegate));
                }
            }
            catch (Exception ex)
            {
                throw new CallTargetInvokerException(ex);
            }
            finally
            {
                if (_invokeDelegate is null)
                {
                    _invokeDelegate = (TTarget instance, ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3) => CallTargetState.GetDefault();
                }
            }
        }

        internal delegate CallTargetState InvokeDelegate(TTarget instance, ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CallTargetState Invoke(TTarget instance, ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3)
        {
            return new CallTargetState(Tracer.Instance.InternalActiveScope, Tracer.Instance.DistributedSpanContext, _invokeDelegate(instance, ref arg1, ref arg2, ref arg3));
        }
    }
}
