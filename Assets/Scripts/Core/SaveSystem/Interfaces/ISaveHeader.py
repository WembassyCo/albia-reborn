"""
ISaveHeader Interface
Defines the metadata structure for save files.
"""
from abc import ABC, abstractmethod
from typing import Dict, Any, Optional
from datetime import datetime
from enum import Enum

class SaveType(Enum):
    """Types of save files."""
    MANUAL = "manual"
    AUTO = "auto"
    QUICK = "quick"
    CHECKPOINT = "checkpoint"

class CompressionType(Enum):
    """Compression algorithms supported."""
    NONE = "none"
    LZ4 = "lz4"
    LZ4_HC = "lz4_hc"  # High compression variant

class ISaveHeader(ABC):
    """
    Interface for save file metadata headers.
    Contains all metadata about a save without the actual world data.
    """
    
    # Class constants for format versioning
    CURRENT_FORMAT_VERSION = 1
    MAGIC_BYTES = b"ALBIA\x00"
    
    @property
    @abstractmethod
    def magic(self) -> bytes:
        """File format identifier bytes."""
        pass
    
    @property
    @abstractmethod
    def format_version(self) -> int:
        """Version of the save file format structure."""
        pass
    
    @property
    @abstractmethod
    def game_version(self) -> str:
        """Version of the game that created this save."""
        pass
    
    @property
    @abstractmethod
    def save_version(self) -> int:
        """Version of the save data (for migration tracking)."""
        pass
    
    @property
    @abstractmethod
    def save_type(self) -> SaveType:
        """Type of save (manual, auto, etc.)."""
        pass
    
    @property
    @abstractmethod
    def save_name(self) -> str:
        """Human-readable save name."""
        pass
    
    @property
    @abstractmethod
    def timestamp(self) -> datetime:
        """When the save was created."""
        pass
    
    @property
    @abstractmethod
    def playtime_seconds(self) -> float:
        """Total playtime in seconds at save moment."""
        pass
    
    @property
    @abstractmethod
    def world_seed(self) -> int:
        """Seed used for world generation."""
        pass
    
    @property
    @abstractmethod
    def world_bounds(self) -> Dict[str, int]:
        """World boundaries: {'min_x': int, 'max_x': int, 'min_y': int, 'max_y': int, 'min_z': int, 'max_z': int}."""
        pass
    
    @property
    @abstractmethod
    def chunk_count(self) -> int:
        """Number of chunks in this save."""
        pass
    
    @property
    @abstractmethod
    def organism_count(self) -> int:
        """Number of organisms in this save."""
        pass
    
    @property
    @abstractmethod
    def compression_type(self) -> CompressionType:
        """Compression algorithm used."""
        pass
    
    @property
    @abstractmethod
    def compression_level(self) -> int:
        """Compression level (algorithm-specific)."""
        pass
    
    @property
    @abstractmethod
    def uncompressed_size(self) -> int:
        """Total uncompressed data size in bytes."""
        pass
    
    @property
    @abstractmethod
    def compressed_size(self) -> int:
        """Total compressed data size in bytes."""
        pass
    
    @property
    @abstractmethod
    def chunk_table_offset(self) -> int:
        """Byte offset to chunk table in file."""
        pass
    
    @property
    @abstractmethod
    def metadata_offset(self) -> int:
        """Byte offset to JSON metadata section."""
        pass
    
    @abstractmethod
    def to_dict(self) -> Dict[str, Any]:
        """Serialize header to dictionary for JSON storage."""
        pass
    
    @abstractmethod
    def from_dict(self, data: Dict[str, Any]) -> bool:
        """Deserialize header from dictionary."""
        pass
    
    @abstractmethod
    def get_header_size(self) -> int:
        """Return the binary size of this header when serialized."""
        pass

class SaveMetadata:
    """
    Additional metadata for saves - thumbnails, screenshots, player notes, etc.
    Stored as JSON in the save file.
    """
    
    def __init__(self):
        self.thumbnail_data: Optional[bytes] = None  # Small preview image (PNG)
        self.player_notes: str = ""
        self.tags: list = []
        self.custom_data: Dict[str, Any] = {}
        self.snapshot_diffs: list = []  # For incremental saves
        
    def to_dict(self) -> Dict[str, Any]:
        return {
            "has_thumbnail": self.thumbnail_data is not None,
            "thumbnail_size": len(self.thumbnail_data) if self.thumbnail_data else 0,
            "player_notes": self.player_notes,
            "tags": self.tags,
            "custom_data": self.custom_data
        }
    
    def from_dict(self, data: Dict[str, Any]) -> None:
        self.player_notes = data.get("player_notes", "")
        self.tags = data.get("tags", [])
        self.custom_data = data.get("custom_data", {})