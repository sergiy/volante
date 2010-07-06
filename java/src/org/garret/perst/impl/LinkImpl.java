package org.garret.perst.impl;
import  org.garret.perst.*;
import  java.util.Iterator;
import  java.lang.reflect.Array;

public class LinkImpl implements Link { 
    public int size() {
        return used;
    }

    public IPersistent get(int i) {
        if (i < 0 || i >= used) { 
            throw new IndexOutOfBoundsException();
        }
        return loadElem(i);
    }

    public IPersistent getRaw(int i) {
        if (i < 0 || i >= used) { 
            throw new IndexOutOfBoundsException();
        }
        return arr[i];
    }

    public void pin() { 
        for (int i = 0, n = used; i < n; i++) { 
            arr[i] = loadElem(i);
        }
    }

    public void unpin() { 
        for (int i = 0, n = used; i < n; i++) { 
            IPersistent elem = arr[i];
            if (elem != null && !elem.isRaw() && elem.isPersistent()) { 
                IPersistent stub = new Persistent();
                stub.assignOid(elem.getStorage(), elem.getOid(), true);
                arr[i] = stub;
            }
        }
    }
                

    public void set(int i, IPersistent obj) {
        if (i < 0 || i >= used) { 
            throw new IndexOutOfBoundsException();
        }
        arr[i] = obj;
    }

    public void remove(int i) {
        if (i < 0 || i >= used) { 
            throw new IndexOutOfBoundsException();
        }
        used -= 1;
        System.arraycopy(arr, i+1, arr, i, used-i);
        arr[used] = null;
    }

    public void setSize(int newSize) { 
        if (newSize < used) { 
            for (int i = used; --i >= newSize; arr[i] = null);
        } else { 
            reserveSpace(newSize - used);            
        }
        used = newSize;
    }

    void reserveSpace(int len) { 
        if (used + len > arr.length) { 
            IPersistent[] newArr = new IPersistent[used + len > arr.length*2 ? used + len : arr.length*2];
            System.arraycopy(arr, 0, newArr, 0, used);
            arr = newArr;
        }
    }

    public void insert(int i, IPersistent obj) { 
         if (i < 0 || i > used) { 
            throw new IndexOutOfBoundsException();
        }
        reserveSpace(1);
        System.arraycopy(arr, i, arr, i+1, used-i);
        arr[i] = obj;
        used += 1;
    }

    public void add(IPersistent obj) {
        reserveSpace(1);
        arr[used++] = obj;
    }

    public void addAll(IPersistent[] a) {
        addAll(a, 0, a.length);
    }
    
    public void addAll(IPersistent[] a, int from, int length) {
        reserveSpace(length);
        System.arraycopy(a, from, arr, used, length);
        used += length;
    }

    public void addAll(Link link) {        
        int n = link.size();
        reserveSpace(n);
        for (int i = 0, j = used; i < n; i++, j++) { 
            arr[j] = link.getRaw(i);
        }
        used += n;
    }

    public IPersistent[] toArray() {
        IPersistent[] a = new IPersistent[used];
        for (int i = used; --i >= 0;) { 
            a[i] = loadElem(i);
        }
        return a;
    }
    
    public IPersistent[] toArray(IPersistent[] arr) {
        if (arr.length < used) { 
            arr = (IPersistent[])Array.newInstance(arr.getClass().getComponentType(), used);
        }
        for (int i = used; --i >= 0;) { 
            arr[i] = loadElem(i);
        }
        if (arr.length > used) { 
            arr[used] = null;
        }
        return arr;
    }
    
    public boolean contains(IPersistent obj) {
        return indexOf(obj) >= 0;
    }

    public int indexOf(IPersistent obj) {
        for (int i = used; --i >= 0;) {
            if (arr[i].equals(obj)) {
                return i;
            }
        }
        return -1;
    }
    
    public void clear() { 
        for (int i = used; --i >= 0;) { 
            arr[i] = null;
        }
        used = 0;
    }

    static class LinkIterator implements Iterator { 
        private Link link;
        private int  i;

        LinkIterator(Link link) { 
            this.link = link;
        }

        public boolean hasNext() {
            return i < link.size();
        }

        public Object next() { 
            return link.get(i++);
        }

        public void remove() {
            link.remove(i);
        }
    }

    public Iterator iterator() { 
        return new LinkIterator(this);
    }

    private final IPersistent loadElem(int i) 
    {
        IPersistent elem = arr[i];
        if (elem.isRaw()) { 
            // arr[i] = elem = ((StorageImpl)elem.getStorage()).lookupObject(elem.getOid(), null);
            elem = ((StorageImpl)elem.getStorage()).lookupObject(elem.getOid(), null);
        }
        return elem;
    }

    LinkImpl() {}

    LinkImpl(int initSize) {
        this.arr = new IPersistent[initSize];
    }

    LinkImpl(IPersistent[] arr) { 
        this.arr = arr;
        used = arr.length;
    }

    IPersistent[] arr;
    int           used;
}
