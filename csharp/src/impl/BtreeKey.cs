namespace Perst.Impl
{
    using System;
    using Perst;
	
    class BtreeKey
    {
        internal Key key;
        internal int oid;
		
        internal BtreeKey(Key key, int oid)
        {
            this.key = key;
            this.oid = oid;
        }
		
        internal void  getStr(Page pg, int i)
        {
            int len = BtreePage.getKeyStrSize(pg, i);
            int offs = BtreePage.firstKeyOffs + BtreePage.getKeyStrOffs(pg, i);
            char[] sval = new char[len];
            for (int j = 0; j < len; j++)
            {
                sval[j] = (char) Bytes.unpack2(pg.data, offs);
                offs += 2;
            }
            key = new Key(sval);
        }
		
        internal void getByteArray(Page pg, int i) 
        { 
            int len = BtreePage.getKeyStrSize(pg, i);
            int offs = BtreePage.firstKeyOffs + BtreePage.getKeyStrOffs(pg, i);
            byte[] bval = new byte[len];
            Array.Copy(pg.data, offs, bval, 0, len);
            key = new Key(bval);
        }
		
        internal void  extract(Page pg, int offs, ClassDescriptor.FieldType type)
        {
            byte[] data = pg.data;
			
            switch (type)
            {
                case ClassDescriptor.FieldType.tpBoolean: 
                    key = new Key(data[offs] != 0);
                    break;
				
                case ClassDescriptor.FieldType.tpSByte: 
                    key = new Key((sbyte)data[offs]);
                    break;
                case ClassDescriptor.FieldType.tpByte: 
                    key = new Key(data[offs]);
                    break;
				
                case ClassDescriptor.FieldType.tpShort: 
                    key = new Key(Bytes.unpack2(data, offs));
                    break;
                case ClassDescriptor.FieldType.tpUShort: 
                    key = new Key((ushort)Bytes.unpack2(data, offs));
                    break;
				
				
                case ClassDescriptor.FieldType.tpChar: 
                    key = new Key((char) Bytes.unpack2(data, offs));
                    break;
				
                case ClassDescriptor.FieldType.tpInt: 
                    key = new Key(Bytes.unpack4(data, offs));
                    break;
                case ClassDescriptor.FieldType.tpEnum: 
                case ClassDescriptor.FieldType.tpUInt: 
                case ClassDescriptor.FieldType.tpObject: 
                    key = new Key((uint)Bytes.unpack4(data, offs));
                    break;
				
                case ClassDescriptor.FieldType.tpLong: 
                    key = new Key(Bytes.unpack8(data, offs));
                    break;
                case ClassDescriptor.FieldType.tpDate: 
                case ClassDescriptor.FieldType.tpULong: 
                    key = new Key((ulong)Bytes.unpack8(data, offs));
                    break;
				
                case ClassDescriptor.FieldType.tpFloat: 
                    key = new Key(BitConverter.ToSingle(BitConverter.GetBytes(Bytes.unpack4(data, offs)), 0));
                    break;
				
                case ClassDescriptor.FieldType.tpDouble: 
#if COMPACT_NET_FRAMEWORK 
                    key = new Key(BitConverter.ToDouble(BitConverter.GetBytes(Bytes.unpack8(data, offs)), 0));
#else
                    key = new Key(BitConverter.Int64BitsToDouble(Bytes.unpack8(data, offs)));
#endif
                    break;

                case ClassDescriptor.FieldType.tpGuid:
                {
                    byte[] bits = new byte[16];
                    Array.Copy(data, offs, bits, 0, 16);
                    key = new Key(new Guid(bits));
                    break;
                }
                case ClassDescriptor.FieldType.tpDecimal:
                {
                    int[] bits = new int[4];
                    bits[0] = Bytes.unpack4(data, offs);
                    bits[1] = Bytes.unpack4(data, offs+4);
                    bits[2] = Bytes.unpack4(data, offs+8);
                    bits[3] = Bytes.unpack4(data, offs+12);
                    key = new Key(new decimal(bits));
                    break;
                }

                default: 
                    Assert.Failed("Invalid type");
                    break;
				
            }
        }
		
        internal void  pack(Page pg, int i)
        {
            byte[] dst = pg.data;
            switch (key.type)
            {
                case ClassDescriptor.FieldType.tpBoolean: 
                case ClassDescriptor.FieldType.tpSByte: 
                case ClassDescriptor.FieldType.tpByte: 
                    dst[BtreePage.firstKeyOffs + i] = (byte) key.ival;
                    break;
				
                case ClassDescriptor.FieldType.tpShort: 
                case ClassDescriptor.FieldType.tpUShort: 
                case ClassDescriptor.FieldType.tpChar: 
                    Bytes.pack2(dst, BtreePage.firstKeyOffs + i * 2, (short) key.ival);
                    break;
				
                case ClassDescriptor.FieldType.tpInt: 
                case ClassDescriptor.FieldType.tpUInt: 
                case ClassDescriptor.FieldType.tpEnum: 
                case ClassDescriptor.FieldType.tpObject: 
                    Bytes.pack4(dst, BtreePage.firstKeyOffs + i * 4, key.ival);
                    break;
				
                case ClassDescriptor.FieldType.tpLong: 
                case ClassDescriptor.FieldType.tpULong: 
                case ClassDescriptor.FieldType.tpDate: 
                    Bytes.pack8(dst, BtreePage.firstKeyOffs + i * 8, key.lval);
                    break;
				
                case ClassDescriptor.FieldType.tpFloat: 
                    Bytes.pack4(dst, BtreePage.firstKeyOffs + i * 4, BitConverter.ToInt32(BitConverter.GetBytes((float)key.dval), 0));
                    break;
				
                case ClassDescriptor.FieldType.tpDouble: 
#if COMPACT_NET_FRAMEWORK 
                    Bytes.pack8(dst, BtreePage.firstKeyOffs + i * 8, BitConverter.ToInt64(BitConverter.GetBytes((double)key.dval), 0));
#else
                    Bytes.pack8(dst, BtreePage.firstKeyOffs + i * 8, BitConverter.DoubleToInt64Bits(key.dval));
#endif
                    break;
				
                case ClassDescriptor.FieldType.tpDecimal:
                {
                    int[] bits = Decimal.GetBits(key.dec);
                    for (int j = 0; j < 4; j++) 
                    { 
                        Bytes.pack4(dst, BtreePage.firstKeyOffs + i*16 + j*4, bits[j]);
                    }
                    break;
                }

                case ClassDescriptor.FieldType.tpGuid:
                    Array.Copy(dst, BtreePage.firstKeyOffs + i*16, key.guid.ToByteArray(), 0, 16);
                    break;

                default: 
                    Assert.Failed("Invalid type");
                    break;
				
            }
            Bytes.pack4(dst, BtreePage.firstKeyOffs + (BtreePage.maxItems - i - 1) * 4, oid);
        }
    }
}