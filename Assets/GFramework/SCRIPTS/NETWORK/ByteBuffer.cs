﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 长度信息变量类型
/// </summary>
public enum LengthMsgDataType : int
{
    INT16 = 2,
    INT32 = 4
}

public class ByteBuffer
{
    public byte[] bufferData;

    private const int DEFAULT_SIZE = 1024;
    private int capacity = 0;
    private int initSize = 0;
    private int readIdx = 0;
    private int writeIdx = 0;

    private LengthMsgDataType type = LengthMsgDataType.INT16;

    /// <summary>
    /// 某个字节[无索引边界检测]
    /// </summary>
    /// <param name="index"></param>
    /// <returns>某个字节</returns>
    public byte this[int index]
    {
        get
        {
            return bufferData[index];
        }
        set
        {
            bufferData[index] = value;
        }
    }

    /// <summary>
    /// 当前数据的长度
    /// </summary>
    public int DataLength
    {
        get
        {
            return writeIdx - readIdx;
        }
    }

    /// <summary>
    /// 缓冲剩余的容量大小
    /// </summary>
    public int RemainLength
    {
        get
        {
            return capacity - writeIdx;
        }
    }

    /// <summary>
    /// 初始化字节缓冲
    /// </summary>
    /// <param name="size">初始化的容量大小</param>
    public ByteBuffer(int size = DEFAULT_SIZE)
    {
        if (size <= 0)
        {
            GDebug.Instance.Warn("初始化的字节缓冲或许不合法");
            return;
        }

        capacity = size;
        initSize = size;
        readIdx = 0;
        writeIdx = 0;
        bufferData = new byte[size];
    }

    /// <summary>
    /// 初始化字节缓冲
    /// </summary>
    /// <param name="data">初始化的字节缓冲的初始数据</param>
    public ByteBuffer(byte[] data)
    {
        if(data==null)
        {
            GDebug.Instance.Warn("初始化字节缓冲传入的字节数组为空");
            return;
        }

        if (data.Length == 0)
        {
            GDebug.Instance.Warn("初始化的字节缓冲或许不合法");
            return;
        }

        capacity = data.Length;
        initSize = data.Length;
        readIdx = 0;
        writeIdx = data.Length;
        bufferData = new byte[capacity];
        Array.Copy(data, 0, bufferData, 0, capacity);
    }

    /// <summary>
    /// 从缓冲中读取完整的信息数据
    /// </summary>
    /// <param name="gets">获取的Byte数组</param>
    /// <param name="offset">读入的偏移量</param>
    /// <param name="readCount">[out]此次读取获得的数组长度</param>
    /// <returns>本次数据读取是否是一条完整的信息</returns>
    public bool Read(byte[] gets,int offset,out int readCount)
    {
        readCount = 0;
        if (DataLength < (int)type)
            return false;

        int msg_symbol_length = (int)type;         //长度信息字节数
        int msg_length = GetMsgLengthFromBytes();  //获取此条完整信息的长度

        int holeDataLength = Math.Min(msg_length + msg_symbol_length, DataLength);  //完整的数据长度（包含长度标志）
        readCount = holeDataLength - msg_symbol_length;                             //此次调用读取时能够获取的最大长度
        Array.Copy(bufferData, readIdx+ msg_symbol_length, gets, offset, readCount);
        if (msg_length == readCount)
        {
            //此次读取的是完整信息，需要更新缓存中的索引变量
            readIdx += holeDataLength;
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// 向字节缓冲写入字节数组
    /// </summary>
    /// <param name="data"></param>
    public void Write(byte[] data)
    {
        if(data.Length==0)
        {
            GDebug.Instance.Warn("当前想要写入的字节数组为0！将不写入缓冲中");
            return;
        }

        int length = data.Length;
        if (readIdx + RemainLength < length)
            ExpanCapacity(length);
        else
            RevertBuffer();  //仅需要重新分配就可以获得足够的空间

        Array.Copy(data, 0, bufferData, DataLength, writeIdx);
        writeIdx += length;
    }

    /// <summary>
    /// 获取当前最新完整数据的长度
    /// </summary>
    /// <returns>若返回0，则意味着数据未完成传输</returns>
    public int GetMsgLengthFromBytes()
    {
        if (DataLength < (int)type)
            return 0;

        if (type == LengthMsgDataType.INT16)
            return (bufferData[1 + readIdx] << 8) | bufferData[0 + readIdx];
        else
            return (bufferData[3 + readIdx] << 24) | 
                   (bufferData[2 + readIdx] << 16) | 
                   (bufferData[1 + readIdx] << 8)  | 
                    bufferData[0 + readIdx];
    }

    /// <summary>
    /// 完全清除此缓冲并恢复原来容量
    /// </summary>
    public void Clear()
    {
        capacity = initSize;
        readIdx = 0;
        writeIdx = 0;
        bufferData = new byte[initSize];
    }

    /// <summary>
    /// 拓展buffer的容量
    /// </summary>
    /// <param name="targetSize">需要添加的目标数组长度</param>
    private void ExpanCapacity(int targetSize)
    {
        while (capacity < targetSize + DataLength) capacity *= 2;
        byte[] newBuffer = new byte[capacity];
        Array.Copy(bufferData, readIdx, newBuffer, 0, DataLength);
        readIdx = 0;
        writeIdx = DataLength;
        bufferData = newBuffer;
    }

    /// <summary>
    /// 重新分配缓冲取的数据
    /// </summary>
    private void RevertBuffer()
    {
        Array.Copy(bufferData, readIdx, bufferData, 0, DataLength);
        readIdx = 0;
        writeIdx = DataLength;
    }
}