package org.garret.perst;

/**
 * Base class for persistent capable objects supporting locking
 */

public class PersistentResource extends Persistent implements IResource {
    public synchronized void sharedLock() {
        Thread currThread = Thread.currentThread();
        try { 
            while (true) { 
                if (owner == currThread) { 
                    nWriters += 1;
                    break;
                } else if (nWriters == 0) { 
                    nReaders += 1;
                    break;
                } else { 
                    wait();
                }
            }
        } catch (InterruptedException x) { 
            throw new StorageError(StorageError.LOCK_FAILED);
        }
    }
                    
    public boolean sharedLock(long timeout) {
        Thread currThread = Thread.currentThread();
        long startTime = System.currentTimeMillis();
        synchronized (this) { 
            try { 
                while (true) { 
                    if (owner == currThread) { 
                        nWriters += 1;
                        return true;
                    } else if (nWriters == 0) { 
                        nReaders += 1;
                        return true;
                    } else { 
                        long currTime = System.currentTimeMillis();
                        if (startTime + timeout <= currTime) { 
                            return false;
                        }
                        wait(startTime + timeout - currTime);
                    }
                }
            } catch (InterruptedException x) { 
                return false;
            }
        }
    }
                    
    public synchronized void exclusiveLock() {
        Thread currThread = Thread.currentThread();
        try { 
            while (true) { 
                if (owner == currThread) { 
                    nWriters += 1;
                    break;
                } else if (nReaders == 0 && nWriters == 0) { 
                    nWriters = 1;
                    owner = currThread;
                    break;
                } else { 
                    wait();
                }
            }
        } catch (InterruptedException x) { 
            throw new StorageError(StorageError.LOCK_FAILED);
        }
    }
                    
    public boolean exclusiveLock(long timeout) {
        Thread currThread = Thread.currentThread();
        long startTime = System.currentTimeMillis();
        synchronized (this) { 
            try { 
                while (true) { 
                    if (owner == currThread) { 
                        nWriters += 1;
                        return true;
                    } else if (nReaders == 0 && nWriters == 0) { 
                        nWriters = 1;
                        owner = currThread;
                        return true;
                    } else { 
                        long currTime = System.currentTimeMillis();
                        if (startTime + timeout <= currTime) { 
                            return false;
                        }
                        wait(startTime + timeout - currTime);
                    }
                }
            } catch (InterruptedException x) { 
                return false;
            }
        }
    }
                   
    public synchronized void unlock() { 
        if (nWriters != 0) { 
            if (--nWriters == 0) { 
                owner = null;
                notifyAll();
            }
        } else { 
            if (--nReaders == 0) { 
                notifyAll();
            }
        }
    }

    private transient Thread owner;
    private transient int    nReaders;
    private transient int    nWriters;
}