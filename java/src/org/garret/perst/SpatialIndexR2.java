package org.garret.perst;

/**
 * Interface of object spatial index.
 * Spatial index is used to allow fast selection of spatial objects belonging to the specified rectangle.
 * Spatial index is implemented using Guttman R-Tree with quadratic split algorithm.
 */
public interface SpatialIndexR2 extends IPersistent, IResource { 
    /**
     * Find all objects located in the selected rectangle
     * @param r selected rectangle
     * @return array of objects which enveloping rectangle intersects with specified rectangle
     */
    public IPersistent[] get(RectangleR2 r);
    
    /**
     * Put new object in the index. 
     * @param r enveloping rectangle for the object
     * @param obj object associated with this rectangle. Object can be not yet persistent, in this case
     * its forced to become persistent by assigning OID to it.
     */
    public void put(RectangleR2 r, IPersistent obj);

    /**
     * Remove object with specified enveloping rectangle from the tree.
     * @param r enveloping rectangle for the object
     * @param obj object removed from the index
     * @exception StorageError(StorageError.KEY_NOT_FOUND) exception if there is no such key in the index
     */
    public void remove(RectangleR2 r, IPersistent obj);

    /**
     * Get number of objects in the index
     * @return number of objects in the index
     */
    public int  size();


    /**
     * Get wrapping rectangle 
     * @return minimal rectangle containing all rectangles in the index, <code>null</code> if index is empty     
     */
    public RectangleR2 getWrappingRectangle();
    
    /**
     * Get itertator for objects located in the selected rectangle
     * @param r selected rectangle
     * @return iterator for objects which enveloping rectangle overlaps with specified rectangle
     */
    public java.util.Iterator iterator(RectangleR2 r);

    /**
     * Remove all objects from the index
     */
    public void clear();
}
