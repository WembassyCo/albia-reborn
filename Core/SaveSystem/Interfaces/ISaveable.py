"""
ISaveable Interface
Defines the contract for any object that can be saved/loaded in the Albia world.
"""
from abc import ABC, abstractmethod
from typing import BinaryIO, Dict, Any

class ISaveable(ABC):
    """
    Interface for objects that can be serialized to and deserialized from save data.
    All world entities (terrain, organisms, environment) must implement this.
    """
    
    @property
    @abstractmethod
    def save_id(self) -> str:
        """Unique identifier for this object in the save system."""
        pass
    
    @property
    @abstractmethod
    def save_version(self) -> int:
        """Version number of this object's save format. Increment on breaking changes."""
        pass
    
    @abstractmethod
    def serialize(self) -> Dict[str, Any]:
        """
        Serialize object state to dictionary format.
        Complex binary data should return a reference to be written separately.
        """
        pass
    
    @abstractmethod
    def deserialize(self, data: Dict[str, Any], binary_reader: BinaryIO = None) -> bool:
        """
        Restore object state from dictionary.
        Returns True if successful, False otherwise.
        """
        pass
    
    @abstractmethod
    def get_binary_size(self) -> int:
        """Return expected binary data size (0 if no binary data)."""
        pass
    
    @abstractmethod
    def write_binary(self, writer: BinaryIO) -> int:
        """Write binary data to output stream. Returns bytes written."""
        pass
    
    @abstractmethod
    def read_binary(self, reader: BinaryIO, size: int) -> bool:
        """Read binary data from input stream. Returns True if successful."""
        pass
    
    def on_pre_save(self) -> None:
        """Called before serialization - hook for cleanup/finalization."""
        pass
    
    def on_post_load(self) -> None:
        """Called after deserialization - hook for initialization/reconstruction."""
        pass

class ISaveableCollection(ISaveable):
    """Extension for objects that contain other ISaveable instances."""
    
    @abstractmethod
    def get_saveable_children(self) -> list:
        """Returns list of child ISaveable objects."""
        pass
    
    @abstractmethod
    def register_saveable(self, obj: ISaveable) -> None:
        """Register a child saveable object."""
        pass
    
    @abstractmethod
    def unregister_saveable(self, save_id: str) -> bool:
        """Unregister a child saveable by ID."""
        pass