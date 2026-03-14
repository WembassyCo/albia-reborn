"""
LZ4 Compression Module
High-performance compression for voxel data using LZ4 algorithm.
Provides both fast and high-compression variants.
"""
import struct
from typing import Union, Tuple, Optional
from io import BytesIO
import logging

# Configure logging
logger = logging.getLogger(__name__)

# Try to import lz4, fallback to zlib if not available
try:
    import lz4.frame as lz4_frame
    import lz4.block as lz4_block
    LZ4_AVAILABLE = True
    logger.info("LZ4 compression available")
except ImportError:
    import zlib
    LZ4_AVAILABLE = False
    logger.warning("LZ4 not available, falling back to zlib")

class LZ4Compression:
    """
    LZ4 compression wrapper for voxel data.
    Optimized for speed and reasonable compression ratios.
    """
    
    # Compression level presets
    LEVEL_FAST = 1       # Speed optimized
    LEVEL_DEFAULT = 3    # Balanced
    LEVEL_BEST = 12      # Size optimized (HC mode)
    
    # Block sizes for different data types
    BLOCK_SIZE_VOXEL = 64 * 64 * 64 * 2  # 64^3 chunks with 2 bytes per voxel
    BLOCK_SIZE_METADATA = 8192
    
    def __init__(self, level: int = LEVEL_DEFAULT):
        """
        Initialize compressor.
        
        Args:
            level: Compression level (1-12, or use LEVEL_* constants)
        """
        self._level = max(1, min(12, level))
        self._use_hc = self._level >= 9
        self._compression_type = "lz4_hc" if self._use_hc else "lz4"
        
    @property
    def level(self) -> int:
        return self._level
    
    @property
    def compression_type(self) -> str:
        return self._compression_type
    
    def compress(self, data: Union[bytes, bytearray, memoryview]) -> bytes:
        """
        Compress data using LZ4.
        
        Args:
            data: Raw data to compress
            
        Returns:
            Compressed data with 4-byte size header
        """
        if isinstance(data, (bytearray, memoryview)):
            data = bytes(data)
            
        if len(data) == 0:
            return struct.pack('<I', 0)  # 4-byte little-endian size = 0
        
        # Fast path: small data doesn't compress well
        if len(data) < 64:
            return struct.pack('<I', len(data)) + data
        
        if LZ4_AVAILABLE:
            if self._use_hc:
                # LZ4 HC mode for better compression
                compressed = lz4_block.compress(
                    data,
                    mode='high_compression',
                    compression=self._level
                )
            else:
                # Standard LZ4 frame compression
                compressed = lz4_frame.compress(
                    data,
                    compression_level=self._level
                )
        else:
            # Fallback to zlib
            compressed = zlib.compress(data, level=min(9, self._level))
        
        # Prepend uncompressed size for decompression validation
        result = struct.pack('<I', len(data)) + compressed
        return result
    
    def decompress(self, data: bytes) -> Optional[bytes]:
        """
        Decompress data.
        
        Args:
            data: Compressed data with 4-byte size header
            
        Returns:
            Decompressed data or None if decompression failed
        """
        if len(data) < 4:
            logger.error(f"Invalid compressed data: too short ({len(data)} bytes)")
            return None
        
        # Extract original size
        original_size = struct.unpack('<I', data[:4])[0]
        compressed = data[4:]
        
        # Fast path: uncompressed small data
        if original_size == len(compressed):
            return compressed
        
        try:
            if LZ4_AVAILABLE:
                # Try LZ4 frame first (most common)
                try:
                    result = lz4_frame.decompress(compressed)
                except Exception:
                    # Fallback to block decompression (for HC mode)
                    result = lz4_block.decompress(compressed, uncompressed_size=original_size)
            else:
                # Use zlib
                result = zlib.decompress(compressed)
            
            # Validate size
            if len(result) != original_size:
                logger.warning(f"Decompression size mismatch: expected {original_size}, got {len(result)}")
                
            return result
            
        except Exception as e:
            logger.error(f"Decompression failed: {e}")
            return None
    
    def compress_voxels(self, voxel_data: bytes, dimensions: Tuple[int, int, int]) -> bytes:
        """
        Optimized voxel compression pipeline.
        Applies delta encoding before LZ4 for better results.
        
        Args:
            voxel_data: Raw voxel bytes
            dimensions: (width, height, depth) of the voxel chunk
            
        Returns:
            Compressed voxel data with metadata header
        """
        if len(voxel_data) == 0:
            return b'\x00' * 16  # Empty chunk marker
        
        # Apply delta encoding (differs from previous voxel)
        # This significantly improves compression for terrain
        encoded = self._delta_encode(voxel_data)
        
        # Compress with LZ4
        compressed = self.compress(encoded)
        
        # Add voxel metadata header
        header = struct.pack('<III', *dimensions)  # 12 bytes
        flags = 0x01 if len(compressed) < len(voxel_data) else 0x00  # Compressed flag
        header += struct.pack('<I', flags)  # Total header: 16 bytes
        
        return header + compressed
    
    def decompress_voxels(self, data: bytes) -> Optional[Tuple[bytes, Tuple[int, int, int]]]:
        """
        Decompress voxel chunk data.
        
        Args:
            data: Compressed voxel data
            
        Returns:
            Tuple of (voxel_data, dimensions) or None on failure
        """
        if len(data) < 16:
            return None
        
        # Parse header
        dimensions = struct.unpack('<III', data[0:12])
        flags = struct.unpack('<I', data[12:16])[0]
        compressed = data[16:]
        
        # Decompress
        encoded = self.decompress(compressed)
        if encoded is None:
            return None
        
        # Handle uncompressed case
        if (flags & 0x01) == 0:
            return encoded, dimensions
        
        # Delta decode
        voxel_data = self._delta_decode(encoded)
        
        return voxel_data, dimensions
    
    def _delta_encode(self, data: bytes) -> bytes:
        """Apply delta encoding for better compression."""
        if len(data) == 0:
            return data
        
        result = bytearray(len(data))
        result[0] = data[0]
        
        for i in range(1, len(data)):
            result[i] = (data[i] - data[i-1]) & 0xFF
            
        return bytes(result)
    
    def _delta_decode(self, data: bytes) -> bytes:
        """Reverse delta encoding."""
        if len(data) == 0:
            return data
        
        result = bytearray(len(data))
        result[0] = data[0]
        
        for i in range(1, len(data)):
            result[i] = (result[i-1] + data[i]) & 0xFF
            
        return bytes(result)
    
    def get_compression_ratio(self, original: bytes, compressed: bytes) -> float:
        """Calculate compression ratio."""
        original = len(original)
        compressed = len(compressed) - 4  # Subtract size header
        if original == 0:
            return 1.0
        return original / max(compressed, 1)
    
    @staticmethod
    def create_fast() -> 'LZ4Compression':
        """Factory method for fast compression."""
        return LZ4Compression(LZ4Compression.LEVEL_FAST)
    
    @staticmethod
    def create_default() -> 'LZ4Compression':
        """Factory method for balanced compression."""
        return LZ4Compression(LZ4Compression.LEVEL_DEFAULT)
    
    @staticmethod
    def create_best() -> 'LZ4Compression':
        """Factory method for best compression."""
        return LZ4Compression(LZ4Compression.LEVEL_BEST)


class CompressionPool:
    """
    Pool of compressors for concurrent compression operations.
    Thread-safe for use with async operations.
    """
    
    def __init__(self, pool_size: int = 4, level: int = LZ4Compression.LEVEL_DEFAULT):
        self._pool = [LZ4Compression(level) for _ in range(pool_size)]
        self._available = set(range(pool_size))
        
    def acquire(self) -> Optional[LZ4Compression]:
        """Acquire a compressor from the pool."""
        if self._available:
            idx = self._available.pop()
            return self._pool[idx]
        return None
    
    def release(self, compressor: LZ4Compression) -> None:
        """Release a compressor back to the pool."""
        try:
            idx = self._pool.index(compressor)
            self._available.add(idx)
        except ValueError:
            pass  # Not from this pool
    
    def compress_parallel(self, chunks: list) -> list:
        """Compress multiple chunks (placeholder for async implementation)."""
        # This would use threading/multiprocessing in production
        results = []
        for chunk in chunks:
            compressor = self._pool[0]  # Use first compressor
            results.append(compressor.compress_voxels(*chunk))
        return results