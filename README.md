# BenchmarkingFilteringDatagridView
Benchmarking datagridview filtering
![results(results.JPG)

Note:
If you change the collection while filtering ensure not accessing the cells and stuff with string indexer, cause the string indexer try to get the owner column with 
the datagridview property. Thats not possible cause you removed it from the collection, therefore its null. 
If you need to access it anyway build your own cellcollection and get the cell from the owner column by enumerate over the cells.
