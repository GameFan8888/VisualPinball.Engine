﻿// MIT License
//
// Copyright (c) 2022 Timothy Raines
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using Unity.Collections.LowLevel.Unsafe;

namespace VisualPinball.Unity
{
	public readonly unsafe struct UnmanagedArray<T> where T : unmanaged
	{
		private readonly void* _buffer;
		public readonly int Length;

		public UnmanagedArray(void* buffer, int length)
		{
			_buffer = buffer;
			Length = length;
		}

		public T this[int index]
		{
			get {
				if (index < 0 || index >= Length) {
					throw new IndexOutOfRangeException();
				}
				return UnsafeUtility.ReadArrayElement<T>(_buffer, index);
			}
		}

		public ref T GetAsRef(int index)
		{
			if (index < 0 || index >= Length) {
				throw new IndexOutOfRangeException();
			}
			return ref UnsafeUtility.ArrayElementAsRef<T>(_buffer, index);
		}

		public T[] ToArray()
		{
			var array = new T[Length];
			for (var i = 0; i < Length; i++) {
				array[i] = UnsafeUtility.ReadArrayElement<T>(_buffer, i);
			}
			return array;
		}
	}
}
