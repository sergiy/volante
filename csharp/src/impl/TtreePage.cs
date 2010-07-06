namespace Perst.Impl 
{
    using System;
    using System.Collections;

    public class TtreePage : Persistent  
    { 
        const int maxItems = (Page.pageSize-ObjectHeader.Sizeof-4*4)/4;
        const int minItems = maxItems - 2; // minimal number of items in internal node

        TtreePage     left;
        TtreePage     right;
        int           balance;
        int           nItems;
        IPersistent[] item;

        internal class PageReference 
        { 
            internal TtreePage pg;
        
            internal PageReference(TtreePage p) { pg = p; }
        }

        public override bool recursiveLoading() 
        {
            return false;
        }

        TtreePage() {}

        internal TtreePage(IPersistent mbr) 
        { 
            nItems = 1;
            item = new IPersistent[maxItems];
            item[0] = mbr;
        }

        IPersistent loadItem(int i) 
        { 
            IPersistent mbr = item[i];
            mbr.load();
            return mbr;
        }

        internal bool find(PersistentComparator comparator, Object minValue, Object maxValue, ArrayList selection)
        { 
            int l, r, m, n;
            load();
            n = nItems;
            if (minValue != null) 
            { 
                if (comparator.compareMemberWithKey(loadItem(0), minValue) < 0) 
                {	    
                    if (comparator.compareMemberWithKey(loadItem(n-1), maxValue) < 0) 
                    { 
                        if (right != null) 
                        { 
                            return right.find(comparator, minValue, maxValue, selection); 
                        } 
                        return true;
                    }
                    for (l = 0, r = n; l < r;) 
                    { 
                        m = (l + r) >> 1;
                        if (comparator.compareMemberWithKey(loadItem(m), minValue) < 0) 
                        {
                            l = m+1;
                        } 
                        else 
                        { 
                            r = m;
                        }
                    }
                    while (r < n) 
                    { 
                        if (maxValue != null
                            && comparator.compareMemberWithKey(loadItem(r), maxValue) > 0)
                        { 
                            return false;
                        }
                        selection.Add(loadItem(r));
                        r += 1;
                    }
                    if (right != null) 
                    { 
                        return right.find(comparator, minValue, maxValue, selection); 
                    } 
                    return true;	
                }
            }	
            if (left != null) 
            { 
                if (!left.find(comparator, minValue, maxValue, selection)) 
                { 
                    return false;
                }
            }
            for (l = 0; l < n; l++) 
            { 
                if (maxValue != null && comparator.compareMemberWithKey(loadItem(l), maxValue) > 0) 
                {
                    return false;
                }
                selection.Add(loadItem(l));
            }
            if (right != null) 
            { 
                return right.find(comparator, minValue, maxValue, selection);
            }         
            return true;
        }
    
        internal bool contains(PersistentComparator comparator, IPersistent mbr)
        { 
            int l, r, m, n;
            load();
            n = nItems;
            if (comparator.compareMembers(loadItem(0), mbr) < 0) 
            {	    
                if (comparator.compareMembers(loadItem(n-1), mbr) < 0) 
                { 
                    if (right != null) 
                    { 
                        return right.contains(comparator, mbr); 
                    } 
                    return false;
                }
                for (l = 0, r = n; l < r;) 
                { 
                    m = (l + r) >> 1;
                    if (comparator.compareMembers(loadItem(m), mbr) < 0) 
                    {
                        l = m+1;
                    } 
                    else 
                    { 
                        r = m;
                    }
                }
                while (r < n) 
                { 
                    if (mbr == loadItem(r)) 
                    { 
                        return true;
                    }
                    if (comparator.compareMembers(item[r], mbr) > 0) 
                    { 
                        return false;
                    }
                    r += 1;
                }
                if (right != null) 
                { 
                    return right.contains(comparator, mbr); 
                } 
                return false;	
            }
            if (left != null) 
            { 
                if (left.contains(comparator, mbr)) 
                { 
                    return true;
                }
            }
            for (l = 0; l < n; l++) 
            { 
                if (mbr == loadItem(l)) 
                { 
                    return true;
                }
                if (comparator.compareMembers(item[l], mbr) > 0) 
                {
                    return false;
                }
            }
            if (right != null) 
            { 
                return right.contains(comparator, mbr);
            }         
            return false;
        }

    
        internal const int OK         = 0;
        internal const int NOT_UNIQUE = 1;
        internal const int NOT_FOUND  = 2;
        internal const int OVERFLOW   = 3;
        internal const int UNDERFLOW  = 4;

        internal int insert(PersistentComparator comparator, IPersistent mbr, bool unique, PageReference pgRef) 
        { 
            load();
            int n = nItems;
            TtreePage pg;
            int diff = comparator.compareMembers(mbr, loadItem(0));
            if (diff <= 0) 
            { 
                if (unique && diff == 0) 
                { 
                    return NOT_UNIQUE;
                }
                if ((left == null || diff == 0) && n != maxItems) 
                { 
                    modify();
                    for (int i = n; i > 0; i--) item[i] = item[i-1];
                    item[0] = mbr;
                    nItems += 1;
                    return OK;
                } 
                if (left == null) 
                { 
                    modify();
                    left = new TtreePage(mbr);
                } 
                else 
                {
                    pg = pgRef.pg;
                    pgRef.pg = left;
                    int result = left.insert(comparator, mbr, unique, pgRef);
                    if (result == NOT_UNIQUE) 
                    { 
                        return NOT_UNIQUE;
                    }
                    modify();
                    left = pgRef.pg;
                    pgRef.pg = pg;
                    if (result == OK) return OK;
                }
                if (balance > 0) 
                { 
                    balance = 0;
                    return OK;
                } 
                else if (balance == 0) 
                { 
                    balance = -1;
                    return OVERFLOW;
                } 
                else 
                { 
                    TtreePage lp = this.left;
                    lp.load();
                    lp.modify();
                    if (lp.balance < 0) 
                    { // single LL turn
                        this.left = lp.right;
                        lp.right = this;
                        balance = 0;
                        lp.balance = 0;
                        pgRef.pg = lp;
                    } 
                    else 
                    { // double LR turn
                        TtreePage rp = lp.right;
                        rp.load();
                        rp.modify();
                        lp.right = rp.left;
                        rp.left = lp;
                        this.left = rp.right;
                        rp.right = this;
                        balance = (rp.balance < 0) ? 1 : 0;
                        lp.balance = (rp.balance > 0) ? -1 : 0;
                        rp.balance = 0;
                        pgRef.pg = rp;
                    }
                    return OK;
                }
            } 
            diff = comparator.compareMembers(mbr, loadItem(n-1));
            if (diff >= 0) 
            { 
                if (unique && diff == 0) 
                { 
                    return NOT_UNIQUE;
                }
                if ((right == null || diff == 0) && n != maxItems) 
                { 
                    modify();
                    item[n] = mbr;
                    nItems += 1;
                    return OK;
                }
                if (right == null) 
                { 
                    modify();
                    right = new TtreePage(mbr);
                } 
                else 
                { 
                    pg = pgRef.pg;
                    pgRef.pg = right;
                    int result = right.insert(comparator, mbr, unique, pgRef);
                    if (result == NOT_UNIQUE) 
                    { 
                        return NOT_UNIQUE;
                    }
                    modify();
                    right = pgRef.pg;
                    pgRef.pg = pg;
                    if (result == OK) return OK;
                }
                if (balance < 0) 
                { 
                    balance = 0;
                    return OK;
                } 
                else if (balance == 0) 
                { 
                    balance = 1;
                    return OVERFLOW;
                } 
                else 
                { 
                    TtreePage rp = this.right;
                    rp.load();
                    rp.modify();
                    if (rp.balance > 0) 
                    { // single RR turn
                        this.right = rp.left;
                        rp.left = this;
                        balance = 0;
                        rp.balance = 0;
                        pgRef.pg = rp;
                    } 
                    else 
                    { // double RL turn
                        TtreePage lp = rp.left;
                        lp.load();
                        lp.modify();
                        rp.left = lp.right;
                        lp.right = rp;
                        this.right = lp.left;
                        lp.left = this;
                        balance = (lp.balance > 0) ? -1 : 0;
                        rp.balance = (lp.balance < 0) ? 1 : 0;
                        lp.balance = 0;
                        pgRef.pg = lp;
                    }
                    return OK;
                }
            }
            int l = 1, r = n-1;
            while (l < r)  
            {
                int i = (l+r) >> 1;
                diff = comparator.compareMembers(mbr, loadItem(i));
                if (diff > 0) 
                { 
                    l = i + 1;
                } 
                else 
                { 
                    r = i;
                    if (diff == 0) 
                    { 
                        if (unique) 
                        { 
                            return NOT_UNIQUE;
                        }
                        break;
                    }
                }
            }
            // Insert before item[r]
            modify();
            if (n != maxItems) 
            {
                for (int i = n; i > r; i--) item[i] = item[i-1]; 
                item[r] = mbr;
                nItems += 1;
                return OK;
            } 
            else 
            { 
                IPersistent reinsertItem;
                if (balance >= 0) 
                { 
                    reinsertItem = loadItem(0);
                    for (int i = 1; i < r; i++) item[i-1] = item[i]; 
                    item[r-1] = mbr;
                } 
                else 
                { 
                    reinsertItem = loadItem(n-1);
                    for (int i = n-1; i > r; i--) item[i] = item[i-1]; 
                    item[r] = mbr;
                }
                return insert(comparator, reinsertItem, unique, pgRef);
            }
        }
       
        internal int balanceLeftBranch(PageReference pgRef) 
        {
            if (balance < 0) 
            { 
                balance = 0;
                return UNDERFLOW;
            } 
            else if (balance == 0) 
            { 
                balance = 1;
                return OK;
            } 
            else 
            { 
                TtreePage rp = this.right;
                rp.load();
                rp.modify();
                if (rp.balance >= 0) 
                { // single RR turn
                    this.right = rp.left;
                    rp.left = this;
                    if (rp.balance == 0) 
                    { 
                        this.balance = 1;
                        rp.balance = -1;
                        pgRef.pg = rp;
                        return OK;
                    } 
                    else 
                    { 
                        balance = 0;
                        rp.balance = 0;
                        pgRef.pg = rp;
                        return UNDERFLOW;
                    }
                } 
                else 
                { // double RL turn
                    TtreePage lp = rp.left;
                    lp.load();
                    lp.modify();
                    rp.left = lp.right;
                    lp.right = rp;
                    this.right = lp.left;
                    lp.left = this;
                    balance = lp.balance > 0 ? -1 : 0;
                    rp.balance = lp.balance < 0 ? 1 : 0;
                    lp.balance = 0;
                    pgRef.pg = lp;
                    return UNDERFLOW;
                }
            }
        }

        internal int balanceRightBranch(PageReference pgRef) 
        {
            if (balance > 0) 
            { 
                balance = 0;
                return UNDERFLOW;
            } 
            else if (balance == 0) 
            { 
                balance = -1;
                return OK;
            } 
            else 
            { 
                TtreePage lp = this.left;
                lp.load();
                lp.modify();
                if (lp.balance <= 0) 
                { // single LL turn
                    this.left = lp.right;
                    lp.right = this;
                    if (lp.balance == 0) 
                    { 
                        balance = -1;
                        lp.balance = 1;
                        pgRef.pg = lp;
                        return OK;
                    } 
                    else 
                    { 
                        balance = 0;
                        lp.balance = 0;
                        pgRef.pg = lp;
                        return UNDERFLOW;
                    }
                } 
                else 
                { // double LR turn
                    TtreePage rp = lp.right;
                    rp.load();
                    rp.modify();
                    lp.right = rp.left;
                    rp.left = lp;
                    this.left = rp.right;
                    rp.right = this;
                    balance = rp.balance < 0 ? 1 : 0;
                    lp.balance = rp.balance > 0 ? -1 : 0;
                    rp.balance = 0;
                    pgRef.pg = rp;
                    return UNDERFLOW;
                }
            }
        }
    
        internal int remove(PersistentComparator comparator, IPersistent mbr, PageReference pgRef)
        {
            load();
            TtreePage pg;
            int n = nItems;
            int diff = comparator.compareMembers(mbr, loadItem(0));
            if (diff <= 0) 
            { 
                if (left != null) 
                { 
                    modify();
                    pg = pgRef.pg;
                    pgRef.pg = left;
                    int h = left.remove(comparator, mbr, pgRef);
                    left = pgRef.pg;
                    pgRef.pg = pg;
                    if (h == UNDERFLOW) 
                    { 
                        return balanceLeftBranch(pgRef);
                    } 
                    else if (h == OK) 
                    { 
                        return OK;
                    }
                }
            }
            diff = comparator.compareMembers(mbr, loadItem(n-1));
            if (diff <= 0) 
            {	    
                for (int i = 0; i < n; i++) 
                { 
                    if (loadItem(i) == mbr) 
                    { 
                        if (n == 1) 
                        { 
                            if (right == null) 
                            { 
                                deallocate();
                                pgRef.pg = left;
                                return UNDERFLOW;
                            } 
                            else if (left == null) 
                            { 
                                deallocate();
                                pgRef.pg = right;
                                return UNDERFLOW;
                            } 
                        }
                        modify();
                        if (n <= minItems) 
                        { 
                            if (left != null && balance <= 0) 
                            {  
                                TtreePage prev = left;
                                prev.load();
                                while (prev.right != null) 
                                {                                 
                                    prev = prev.right;
                                    prev.load();
                                }
                                while (--i >= 0) 
                                { 
                                    item[i+1] = item[i];
                                }
                                item[0] = prev.item[prev.nItems-1];
                                pg = pgRef.pg;
                                pgRef.pg = left;
                                int h = left.remove(comparator, loadItem(0), pgRef);
                                left = pgRef.pg;
                                pgRef.pg = pg;
                                if (h == UNDERFLOW) 
                                {
                                    h = balanceLeftBranch(pgRef);
                                }
                                return h;
                            } 
                            else if (right != null) 
                            { 
                                TtreePage next = right;
                                next.load();
                                while (next.left != null) 
                                { 
                                    next = next.left;
                                    next.load();
                                }
                                while (++i < n) 
                                { 
                                    item[i-1] = item[i];
                                }
                                item[n-1] = next.item[0];
                                pg = pgRef.pg;
                                pgRef.pg = right;
                                int h = right.remove(comparator, loadItem(n-1), pgRef);
                                right = pgRef.pg;
                                pgRef.pg = pg;
                                if (h == UNDERFLOW) 
                                {
                                    h = balanceRightBranch(pgRef);
                                }
                                return h;
                            }
                        }
                        while (++i < n) 
                        { 
                            item[i-1] = item[i];
                        }
                        nItems -= 1;
                        return OK;
                    }
                }
            }
            if (right != null) 
            { 
                modify();
                pg = pgRef.pg;
                pgRef.pg = right;
                int h = right.remove(comparator, mbr, pgRef);
                right = pgRef.pg;
                pgRef.pg = pg;
                if (h == UNDERFLOW) 
                { 
                    return balanceRightBranch(pgRef);
                }
                else 
                { 
                    return h;
                }
            }
            return NOT_FOUND;
        }


        internal int toArray(IPersistent[] arr, int index) 
        { 
            load();
            if (left != null) 
            { 
                index = left.toArray(arr, index);
            }
            for (int i = 0, n = nItems; i < n; i++) 
            { 
                arr[index++] = loadItem(i);
            }
            if (right != null) 
            { 
                index = right.toArray(arr, index);
            }
            return index;
        }

        internal void prune() 
        { 
            load();
            if (left != null) 
            { 
                left.prune();
            }
            if (right != null) 
            { 
                right.prune();
            }
            deallocate();
        }

    }
}