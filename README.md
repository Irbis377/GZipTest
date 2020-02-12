# GZipTest
Test assesment for Veeam

MultiThreading Zipper 

1. This application is useless for a small file because it writes the length and block number in the output file for further correct decompressing.
2. Files size > 1 GB compressed worse than 7z, but faster
	Example: 
				test.vmdk (2,429,288,448 bytes) 
				this app: compress to 1,197,721,240 bytes ~ 26 sec 696 ms, decompress ~26.144
				7z (*7z): compress to 650,081,626 bytes ~ 1 min 41 sec 320 ms, decompress ~31 sec
				
3.Other zippers can't decompress this packed-file   




Main logic
 
Compress:
we read and save blocks in queue with number of order and length of block,		--Read 
then (parallell) dequeue blocks, compress these blocks using GZipStream and adding its to SortedList (by order number)		--Process
And finnaly write length of block and this compressed data to output file from SortedList		--Write

Decompress (same but in reverse order): 
read length (4 bytes) of block then read data of block. Add it into Queue
(parallel) dequeue blocks from queue and decompress it using GZipStream, then add this decompressed data to SortedList with order number,
and finnaly write decompred stream from SortedList to output file
